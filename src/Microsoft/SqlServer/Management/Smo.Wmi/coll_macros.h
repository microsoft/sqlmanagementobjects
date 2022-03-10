#ifndef COLLECTION_SUFFIX
#define COLLECTION_SUFFIX Collection 
#endif

#ifdef SEALED
#define SEALED_IMP sealed
#else
#define SEALED_IMP 
#endif

#define IDENTITY(x) x
#define TOKEN_PASTE(x,y) IDENTITY(x)##IDENTITY(y)
#define TOKEN_PASTE3(x,y,z) IDENTITY(x)##IDENTITY(y)##IDENTITY(z)

#define STRINGIZE(x) #x
#define STRINGER(x) STRINGIZE(x)


#ifndef PARTIAL_KEYWORD
	#define PARTIAL_KEYWORD
#endif
