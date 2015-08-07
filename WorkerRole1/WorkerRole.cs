using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace WorkerRole1
{
    public class CustomerEntity : TableEntity
    {
        public CustomerEntity(Guid RandNum1, Guid RandNum2)
        {
            this.PartitionKey = RandNum1.ToString();
            this.RowKey = RandNum2.ToString();

        }

        public CustomerEntity() { }

        public string PhoneNumber { get; set; }

        public DateTime theNow { get; set; }
    }

    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("ConnectionString"));

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("RandNumbers");
            table.CreateIfNotExists();

            try
            {
                while (true)
                {
                    Random rand = new Random();
                    CustomerEntity customer1 = new CustomerEntity(Guid.NewGuid(), Guid.NewGuid());
                    customer1.PhoneNumber = (rand.Next(500000, 600000)).ToString();
                    customer1.theNow = DateTime.Now;

                    TableOperation insertOperation = TableOperation.Insert(customer1);

                    table.Execute(insertOperation);

                    Console.WriteLine("Line Written to table with random value of" + customer1.PhoneNumber);

                    Thread.Sleep(1500);

                }
                //this.RunAsync(this.cancellationTokenSource.Token).Wait();

            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
