using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace norplan.adm.qrlib
{
    public class RetObj
    {
        public bool Success;
        public object Data;
        public StringBuilder Messages;

        public RetObj(bool pSuccess = true)
        {
            Messages = new StringBuilder();
            Success = pSuccess;
        }

        public void SetSuccess()
        {
            Success = true;
        }

        public void SetError()
        {
            Success = false;
        }

        public void AddMessage(string pMessage)
        {
            Messages.AppendLine(pMessage);
        }

        public string GetMessages()
        {
            return Messages.ToString();
        }
    }
}
