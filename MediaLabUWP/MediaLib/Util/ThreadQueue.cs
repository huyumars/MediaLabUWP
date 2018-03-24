using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaLib
{
    public class ThreadQueue
    {
        public delegate void Task();
        Queue tasks = Queue.Synchronized(new Queue());
        BackgroundWorker backGroudWorker = new BackgroundWorker();
        public ThreadQueue()
        {
            backGroudWorker.DoWork += BackGroudWorker_DoWork;
        }

        private void BackGroudWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (tasks.Count > 0)
            {
                var task = tasks.Dequeue() as Task;
                try
                {
                    task();
                }
                catch (Exception ex) { Logger.ERROR("ThreadQueue task meet error quit! " + ex.Message); }
            }
        }

        public void enqueueTask(Task task)
        {
            tasks.Enqueue(task);
            if (!backGroudWorker.IsBusy)
                backGroudWorker.RunWorkerAsync();
        }
    }
}
