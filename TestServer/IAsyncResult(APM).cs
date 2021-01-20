using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    class IAsyncResult_APM_
    {
        // End메서드를 호출할 Callback 메서드
        public void CalculateDone(IAsyncResult asyncResult)
        {
            var param = asyncResult.AsyncState as object[];
            if (param == null) return;

            var factorial = param[0] as Factorial;
            var input = (int)param[1];
            var result = factorial.EndCalculateFactorial(asyncResult);
            Console.WriteLine("Input : {0}, Calculate Result : {1}", input, result);
        }

        public void something()
        {
            var factorial = new Factorial();
            factorial.BeginCalculateFactorial(5, CalculateDone, factorial);
        }
    }
    
    public class Factorial
    {
        // delegate 생성
        delegate int CalcuateFactorialDelegate(int p);
        private Func<int, int> func;

        public Factorial()
        {
            CalcuateFactorialDelegate d_CalFac = new CalcuateFactorialDelegate(CalculateFactorial);
            this.func = this.CalculateFactorial;
        }

        // 작업을 진행할 메서드
        public int CalculateFactorial(int p)
        {
            if (p <= 0)
            {
                return -1;
            }
            try
            {
                int n = 1;
                for (int i=1; i <= p; i++)
                {
                    n = n * i;
                }
                return n;
            } catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return -1;
            }
        }

        // BeginInvoke를 할 Begin 메서드
        public IAsyncResult BeginCalculateFactorial(int p, AsyncCallback asyncCallback, object state)
        {
            var param = new object[] { state, p };
            return this.func.BeginInvoke(p, asyncCallback, param);
        }

        // EndInvoke를 할 End 메서드
        public int EndCalculateFactorial(IAsyncResult asyncResult)
        {
            return this.func.EndInvoke(asyncResult);
        }
    }
}
