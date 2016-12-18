using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CKAN
{
    public class GtkUser : IUser
    {
        public bool Headless
        {
            get
            {
                return false;
            }
        }

        public int WindowWidth
        {
            get
            {
                return 500;
            }
        }

        public event DisplayYesNoDialog AskUser;
        public event DisplaySelectionDialog AskUserForSelection;
        public event DownloadsComplete DownloadsComplete;
        public event DisplayError Error;
        public event DisplayMessage Message;
        public event ReportProgress Progress;

        public void RaiseDownloadsCompleted(Uri[] file_urls, string[] file_paths, Exception[] errors)
        {
            //
        }

        public void RaiseError(string message, params object[] args)
        {
            //
        }

        public void RaiseMessage(string message, params object[] url)
        {
            //
        }

        public void RaiseProgress(string message, int percent)
        {
            //
        }

        public int RaiseSelectionDialog(string message, params object[] args)
        {
            return 0;
        }

        public bool RaiseYesNoDialog(string question)
        {
            return false;
        }
    }
}
