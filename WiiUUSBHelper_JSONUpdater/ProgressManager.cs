using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiUUSBHelper_JSONUpdater
{
    class ProgressManager
    {
        private int currentStep;
        private int maxSteps;

        public ProgressManager()
        {
            Reset(0);
        }

        public void SetTitle(string title)
        {
            Console.Title = title;
        }

        public void Reset(int maxSteps)
        {
            this.currentStep = 0;
            this.maxSteps = maxSteps;
            Console.WriteLine();
        }

        public void Step(string text = null)
        {
            this.currentStep += 1;
            Print(text);
        }

        private void Print(string text = null)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("[{0}/{1}]", currentStep, maxSteps);
            if (text != null)
                Console.Write(": " + text);
        }
    }
}
