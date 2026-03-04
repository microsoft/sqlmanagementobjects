	///<summary>enum for CLASSNAME</summary>
	internal enum CLASSNAMEValues
	{
		// Generated code
@@@CLASSNAME_enum
		// End of generated code
	}

		///<summary> CLASSNAME</summary>
		public sealed class CLASSNAME
		{
			private CLASSNAMEValues m_value;
	
			///<summary>constructor</summary>
			internal CLASSNAME(CLASSNAMEValues eventValue)
			{
				m_value = eventValue;
			}
	
			///<summary>get value</summary>
			internal CLASSNAMEValues Value
			{
				get { return m_value; }
			}

			///<summary>cast operator</summary>
			static public implicit operator CLASSNAMESet(CLASSNAME eventValue)
			{
				return new CLASSNAMESet(eventValue);
			}
	
			///<summary>add to events resulting an event set</summary>
			static public CLASSNAMESet operator +(CLASSNAME eventLeft, CLASSNAME eventRight)
			{
				CLASSNAMESet eventSet = new CLASSNAMESet(eventLeft);
				eventSet.SetBit(eventRight);
				return eventSet;
			}
	
			///<summary>add to events resulting an event set</summary>
			static public CLASSNAMESet Add(CLASSNAME eventLeft, CLASSNAME eventRight)
			{
				return eventLeft + eventRight;
			}
	
			///<summary>'or' to events resulting an event set</summary>
			static public CLASSNAMESet operator |(CLASSNAME eventLeft, CLASSNAME eventRight)
			{
				CLASSNAMESet eventSet = new CLASSNAMESet(eventLeft);
				eventSet.SetBit(eventRight);
				return eventSet;
			}

			///<summary>'or' to events resulting an event set</summary>
			static public CLASSNAMESet BitwiseOr(CLASSNAME eventLeft, CLASSNAME eventRight)
			{
				return eventLeft | eventRight;
			}
	
			///<summary>event string representation</summary>
			public override string ToString()
			{
				return m_value.ToString();
			}
		// Generated code
@@@CLASSNAME_static_props
		// End of generated code

	}
		///<summary>class CLASSNAMESet </summary>
		public sealed class CLASSNAMESet : EventSetBase
		{
	
			///<summary>default constructor</summary>
			public CLASSNAMESet()
			{
			}
	
			///<summary>copy constructor</summary>
			public CLASSNAMESet(CLASSNAMESet eventSet) : base(eventSet)
			{
			}
	
			///<summary>constructor initialize with an event</summary>
			public CLASSNAMESet(CLASSNAME anEvent)
			{
				SetBit(anEvent);
			}
	
			///<summary>constructor initialize with a list of events</summary>
			public CLASSNAMESet(params CLASSNAME[] events)
			{
				Storage = new BitArray(this.NumberOfElements);
				foreach(CLASSNAME evt in events)
				{
					SetBit(evt);
				}
			}

			///<summary>initialize from BitArray</summary>
			internal CLASSNAMESet(BitArray storage)
			{
				Storage = (BitArray) storage.Clone();
			}

			///<summary>copy</summary>
			public override EventSetBase Copy()
			{
				return new CLASSNAMESet(this.Storage);
			}

			///<summary>set bit for an event</summary>
			internal void SetBit(CLASSNAME anEvent)
			{
				Storage[(int)anEvent.Value] = true;
			}
	
			///<summary>reset bit for an event</summary>
			internal void ResetBit(CLASSNAME anEvent)
			{
				Storage[(int)anEvent.Value] = false;
			}
	
			///<summary>set bit for an event</summary>
			public CLASSNAMESet Add(CLASSNAME anEvent)
			{
				SetBit(anEvent);
				return this;
			}

			///<summary>reset bit for an event</summary>
			public CLASSNAMESet Remove(CLASSNAME anEvent)
			{
				ResetBit(anEvent);
				return this;
			}
	
			///<summary>add an event</summary>
			static public CLASSNAMESet operator +(CLASSNAMESet eventSet, CLASSNAME anEvent)
			{
				CLASSNAMESet newEventSet = new CLASSNAMESet(eventSet);
				newEventSet.SetBit(anEvent);
				return newEventSet;
			}
	
			///<summary>add an event</summary>
			static public CLASSNAMESet Add(CLASSNAMESet eventSet, CLASSNAME anEvent)
			{
				return eventSet + anEvent;
			}
	
			///<summary>remove an event</summary>
			static public CLASSNAMESet operator -(CLASSNAMESet eventSet, CLASSNAME anEvent)
			{
				CLASSNAMESet newEventSet = new CLASSNAMESet(eventSet);
				newEventSet.ResetBit(anEvent);
				return newEventSet;
			}
	
			///<summary>remove an event</summary>
			static public CLASSNAMESet Subtract(CLASSNAMESet eventSet, CLASSNAME anEvent)
			{
				return eventSet - anEvent;
			}
	
			///<summary>return number of elements</summary>
			public override int NumberOfElements
			{
@@@CLASSNAME_elements_count
			}

			///<summary>return string representation</summary>
			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();

				sb.Append(this.GetType().Name + ": ");

				int i = 0;
				bool first = true;
				foreach (bool isSet in Storage)
				{
					if (isSet)
					{
						if (first)
						{
							first = false;
						}
						else
						{
							sb.Append(", ");
						}
						sb.Append(((CLASSNAMEValues) i).ToString());
					}
					i++;
				}
				return sb.ToString();
			}
			private bool dirty = false;
			///<summary>true if the event set has been modified</summary>
			public bool Dirty
			{
				get { return dirty; }
				set { dirty = value; }
			}
		// Generated code
@@@CLASSNAME_props
		// End of generated code


		///<summary>static constructor
		/// init here all the static composite bitflags, because we do not want 
		/// to recreate it every time we will do an operation with it.</summary>
		static CLASSNAMESet()
		{
		// Generated code
@@@CLASSNAME_group_static_props_init
		// End of generated code
			
		}

@@@CLASSNAME_group_static_props
	}
