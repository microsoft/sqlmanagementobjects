

	//
	// CLASSNAME
	//
	public sealed class CLASSNAME
	{
		private CLASSNAMEValues m_value;
	
		internal CLASSNAME(CLASSNAMEValues eventValue)
		{
			m_value = eventValue;
		}
	
		internal CLASSNAMEValues Value
		{
			 get {{ return m_value; }}
		}

		static public implicit operator CLASSNAMESet(CLASSNAME eventValue)
		{
			return new CLASSNAMESet(eventValue);
		}
	
		static public CLASSNAMESet operator +(CLASSNAME eventLeft, CLASSNAME eventRight)
		{
			CLASSNAMESet eventSet = new CLASSNAMESet(eventLeft);
			eventSet.SetBit(eventRight);
			return eventSet;
		}
	
		static public CLASSNAMESet Add(CLASSNAME eventLeft, CLASSNAME eventRight)
		{
			return eventLeft + eventRight;
		}
	
		static public CLASSNAMESet operator |(CLASSNAME eventLeft, CLASSNAME eventRight)
		{
			CLASSNAMESet eventSet = new CLASSNAMESet(eventLeft);
			eventSet.SetBit(eventRight);
			return eventSet;
		}

		static public CLASSNAMESet BitwiseOr(CLASSNAME eventLeft, CLASSNAME eventRight)
		{
			return eventLeft | eventRight;
		}

        public override string ToString()
        {
            return m_value.ToString();
        }

		// Satisfies FxCop rule: AddAndSubtractOverrideShouldHaveOperatorEqualsOverride.
		public static bool operator ==(CLASSNAME a, CLASSNAME b)
		{
			if( null == (a as object) && null == (b as object))
				return true;
			else if( null == (a as object) || null == (b as object))
				return false;
			else
				return a.m_value == b.m_value;
		}

		// If you implement ==, you must implement !=.
		public static bool operator !=(CLASSNAME a, CLASSNAME b)
		{
			return !(a==b);
		}

		// Equals should be consistent with operator ==.
		public override bool Equals(Object obj)
		{
			if (obj == null)
				return false;

			return this == (obj as CLASSNAME);
		}

		public override int GetHashCode()
		{
			return m_value.GetHashCode ();
		}
	
	
		// Generated code
@@@CLASSNAME_static_props
		// End of generated code

	}

	public sealed class CLASSNAMESet : EventSetBase
	{
	
		public CLASSNAMESet()
		{
		}
	
		public CLASSNAMESet(CLASSNAMESet eventSet) : base(eventSet)
		{
		}
	
		public CLASSNAMESet(CLASSNAME anEvent)
		{
			SetBit(anEvent);
		}
	
		public CLASSNAMESet(params CLASSNAME[] events)
		{
			Storage = new BitArray(this.NumberOfElements);
			foreach(CLASSNAME evt in events)
			{
				SetBit(evt);
			}
		}

        public override EventSetBase Copy()
        {
            return new CLASSNAMESet(this.Storage);
        }

        internal CLASSNAMESet(BitArray storage)
        {
            Storage = (BitArray) storage.Clone();
        }

		internal void SetBit(CLASSNAME anEvent)
		{
			 Storage[(int)anEvent.Value] = true;
		}
	
		internal void ResetBit(CLASSNAME anEvent)
		{
			 Storage[(int)anEvent.Value] = false;
		}
	
		public CLASSNAMESet Add(CLASSNAME anEvent)
		{
			 SetBit(anEvent);
			 return this;
		}

		public CLASSNAMESet Remove(CLASSNAME anEvent)
		{
			 ResetBit(anEvent);
			 return this;
		}
	
		static public CLASSNAMESet operator +(CLASSNAMESet eventSet, CLASSNAME anEvent)
		{
			CLASSNAMESet newEventSet = new CLASSNAMESet(eventSet);
			newEventSet.SetBit(anEvent);
			return newEventSet;
		}
	
		static public CLASSNAMESet Add(CLASSNAMESet eventSet, CLASSNAME anEvent)
		{
			return eventSet + anEvent;
		}
	
		static public CLASSNAMESet operator -(CLASSNAMESet eventSet, CLASSNAME anEvent)
		{
			CLASSNAMESet newEventSet = new CLASSNAMESet(eventSet);
			newEventSet.ResetBit(anEvent);
			return newEventSet;
		}
	
		static public CLASSNAMESet Subtract(CLASSNAMESet eventSet, CLASSNAME anEvent)
		{
			return eventSet - anEvent;
		}
	
		public override int NumberOfElements
		{
@@@CLASSNAME_count
		}

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
	
		// Generated code
@@@CLASSNAME_props
		// End of generated code
	}

