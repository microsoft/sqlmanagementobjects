---
applyTo: '**'
---
# EventSource best practices

Use this document as a guide when adding new events to an EventSource.
## Querying Microsoft Documentation

You have access to an MCP server called `microsoft.docs.mcp` - this tool allows you to search through Microsoft's latest official documentation, and that information might be more detailed or newer than what's in your training data set.

When handling questions around how to work with native Microsoft technologies, such as C#, F#, ASP.NET Core, Microsoft.Extensions, NuGet, Entity Framework, the `dotnet` runtime - please use this tool for research purposes when dealing with specific / narrowly defined questions that may occur.

## EventSource Overview
The basic structure of a derived EventSource is always the same. In particular:

The class inherits from System.Diagnostics.Tracing.EventSource
For each different type of event you wish to generate, a method needs to be defined. This method should be named using the name of the event being created. If the event has additional data these should be passed using arguments. These event arguments need to be serialized so only certain types are allowed.
Each method has a body that calls WriteEvent passing it an ID (a numeric value that represents the event) and the arguments of the event method. The ID needs to be unique within the EventSource. The ID is explicitly assigned using the System.Diagnostics.Tracing.EventAttribute
EventSources are intended to be singleton instances. Thus it's convenient to define a static variable, by convention called Log, that represents this singleton.

## EventSource Keywords

Some event tracing systems support keywords as an additional filtering mechanism. Unlike verbosity that categorizes events by level of detail, keywords are intended to categorize events based on other criteria such as areas of code functionality or which would be useful for diagnosing certain problems. Keywords are named bit flags and each event can have any combination of keywords applied to it. For example the EventSource below defines some events that relate to request processing and other events that relate to startup. If a developer wanted to analyze the performance of startup, they might only enable logging the events marked with the startup keyword.
```C#
[EventSource(Name = "Demo")]
class DemoEventSource : EventSource
{
    public static DemoEventSource Log { get; } = new DemoEventSource();

    [Event(1, Keywords = Keywords.Startup)]
    public void AppStarted(string message, int favoriteNumber) => WriteEvent(1, message, favoriteNumber);
    [Event(2, Keywords = Keywords.Requests)]
    public void RequestStart(int requestId) => WriteEvent(2, requestId);
    [Event(3, Keywords = Keywords.Requests)]
    public void RequestStop(int requestId) => WriteEvent(3, requestId);

    public class Keywords   // This is a bitvector
    {
        public const EventKeywords Startup = (EventKeywords)0x0001;
        public const EventKeywords Requests = (EventKeywords)0x0002;
    }
}
```
Keywords must be defined by using a nested class called Keywords and each individual keyword is defined by a member typed public const EventKeywords.

Keywords are more important when distinguishing between high volume events. This allows an event consumer to raise the verbosity to a high level but manage the performance overhead and log size by only enabling narrow subsets of the events. Events that are triggered more than 1,000/sec are good candidates for a unique keyword.
For example, if you have a high volume of events related to database operations, you might want to create a keyword specifically for those events. This way, users can choose to enable or disable all database-related events with a single keyword.

## EventSource Levels
Use levels less than Informational for relatively rare warnings or errors. When in doubt, stick with the default of Informational and use Verbose for events that occur more frequently than 1000 events/sec.

## EventSource methods

### Rules for defining event methods
Any instance, non-virtual, void returning method defined in an EventSource class is by default an event logging method.
Virtual or non-void-returning methods are included only if they're marked with the System.Diagnostics.Tracing.EventAttribute
To mark a qualifying method as non-logging you must decorate it with the System.Diagnostics.Tracing.NonEventAttribute
Event logging methods have event IDs associated with them. This can be done either explicitly by decorating the method with a System.Diagnostics.Tracing.EventAttribute or implicitly by the ordinal number of the method in the class. For example using implicit numbering the first method in the class has ID 1, the second has ID 2, and so on.
Event logging methods must call a WriteEvent, WriteEventCore, WriteEventWithRelatedActivityId or WriteEventWithRelatedActivityIdCore overload.
The event ID, whether implied or explicit, must match the first argument passed to the WriteEvent* API it calls.
The number, types and order of arguments passed to the EventSource method must align with how they're passed to the WriteEvent* APIs. For WriteEvent the arguments follow the Event ID, for WriteEventWithRelatedActivityId the arguments follow the relatedActivityId. For the WriteEvent*Core methods, the arguments must be serialized manually into the data parameter.
Event names cannot contain < or > characters. While user-defined methods also cannot contain these characters, async methods will be rewritten by the compiler to contain them. To be sure these generated methods don't become events, mark all non-event methods on an EventSource with the NonEventAttribute.

### Best practices
Types that derive from EventSource usually don't have intermediate types in the hierarchy or implement interfaces. See Advanced customizations below for some exceptions where this may be useful.
Generally the name of the EventSource class is a bad public name for the EventSource. Public names, the names that will show up in logging configurations and log viewers, should be globally unique. Thus it's good practice to give your EventSource a public name using the System.Diagnostics.Tracing.EventSourceAttribute. The name "Demo" used above is short and unlikely to be unique so not a good choice for production use. A common convention is to use a hierarchical name with . or - as a separator, such as "MyCompany-Samples-Demo", or the name of the Assembly or namespace for which the EventSource provides events. It's not recommended to include "EventSource" as part of the public name.
Assign Event IDs explicitly, this way seemingly benign changes to the code in the source class such as rearranging it or adding a method in the middle won't change the event ID associated with each method.
When authoring events that represent the start and end of a unit of work, by convention these methods are named with suffixes 'Start' and 'Stop'. For example, 'RequestStart' and 'RequestStop'.
Do not specify an explicit value for EventSourceAttribute’s Guid property, unless you need it for backwards compatibility reasons. The default Guid value is derived from the source’s name, which allows tools to accept the more human-readable name and derive the same Guid.
Call IsEnabled() before performing any resource intensive work related to firing an event, such as computing an expensive event argument that won't be needed if the event is disabled.
Attempt to keep EventSource object back compatible and version them appropriately. The default version for an event is 0. The version can be changed by setting EventAttribute.Version. Change the version of an event whenever you change the data that is serialized with it. Always add new serialized data to the end of the event declaration, that is, at the end of the list of method parameters. If this isn't possible, create a new event with a new ID to replace the old one.
When declaring events methods, specify fixed-size payload data before variably sized data.
Do not use strings containing null characters. When generating the manifest for ETW EventSource will declare all strings as null terminated, even though it is possible to have a null character in a C# String. If a string contains a null character the entire string will be written to the event payload, but any parser will treat the first null character as the end of the string. If there are payload arguments after the string, the remainder of the string will be parsed instead of the intended value.

### Performance Considerations

**CRITICAL: Always check IsEnabled() before expensive operations**

Event method call sites should always call `Log.IsEnabled(level, keyword)` before performing any potentially complex calculations, string formatting, or other expensive operations to avoid introducing performance regressions when tracing is disabled.

When events are disabled (which is the default state), the EventSource infrastructure is highly optimized and calling event methods has minimal overhead. However, any work done to prepare event arguments (such as string formatting, collection enumeration, or complex calculations) will still be executed even when events are disabled.

**Example of INCORRECT usage (performance regression):**
```C#
// BAD: This will always execute the expensive operation, even when events are disabled
string expensiveData = GenerateExpensiveReport(); // Always executes!
SmoEventSource.Log.DatabaseOperation("Operation completed", expensiveData);
```

**Example of CORRECT usage (performance optimized):**
```C#
// GOOD: Only execute expensive operations when events are actually enabled
if (SmoEventSource.Log.IsEnabled(EventLevel.Informational, Keywords.Database))
{
    string expensiveData = GenerateExpensiveReport(); // Only executes when needed
    SmoEventSource.Log.DatabaseOperation("Operation completed", expensiveData);
}
```

**Additional performance guidelines:**
- Use `IsEnabled(EventLevel level, EventKeywords keywords)` to check both level and keyword filters
- For high-frequency events (>1000/sec), always use IsEnabled() checks
- Keep event argument preparation lightweight when IsEnabled() checks aren't practical
- Truncate large strings before passing to event methods to avoid excessive ETW payload sizes
- Consider using separate events for different verbosity levels rather than complex conditional logic

**Example with level and keyword checking:**
```C#
// Check both level and keywords for precise control
if (SmoEventSource.Log.IsEnabled(EventLevel.Verbose, Keywords.Performance))
{
    var queryText = TruncateSqlForLogging(command.CommandText, 200);
    var duration = stopwatch.ElapsedMilliseconds;
    SmoEventSource.Log.QueryExecutionCompleted(queryText, duration, rowsAffected);
}
```

### Supported parameter types

EventSource requires that all event parameters can be serialized so it only accepts a limited set of types. These are:

- Primitives: bool, byte, sbyte, char, short, ushort, int, uint, long, ulong, float, double, IntPtr, and UIntPtr, Guid decimal, string, DateTime, DateTimeOffset, TimeSpan
- Enums
- Structures attributed with System.Diagnostics.Tracing.EventDataAttribute. Only the public instance properties with serializable types will be serialized.
- Anonymous types where all public properties are serializable types
- Arrays of serializable types
- Nullable<T> where T is a serializable type
- KeyValuePair<T, U> where T and U are both serializable types
- Types that implement IEnumerable<T> for exactly one type T and where T is a serializable type