using System;

namespace MSNPSharpClient
{
    public class UniversalEventArgs : EventArgs
    {
        private object param = null;
            
        public object Param
        {
            get
            {
                return param;
            }
            
            private set 
            {
                param = value;
            }
        }
        
        public UniversalEventArgs(object param)
        {
            Param = param;
        }
    }
}

