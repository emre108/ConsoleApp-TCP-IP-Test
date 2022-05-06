using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Text;



namespace UI_1._0_ServerConsoleAppTFR
{
    class Program
    {

        public static UInt32 RAM_BUFFER_SIZE = 2042400;
        public static UInt32 PacketSize = 60;
        public static UInt32 SizePerCycle = 6;
        public static class Globals
        {
            public static bool MCM_Connected = false;
            public static bool RecordGetFlag = true;
            public static bool NormalFlag = false;
            public static bool TimeShow = true;
            public static bool CsvFlag = false;
            public static byte[] CommanUI = { 66, 65, 68, 47, 48, 31, 32, 33, 34, 35, 66, 65, 68, 47, 48, 31, 32, 33, 34, 35 };
            public static float[] transX = new float[/*840*/RAM_BUFFER_SIZE];
            public static byte[] FaultDatas = new byte[PacketSize * 4/*288*//*240*//*480*/];
            public static byte[] Datas = new byte[1];
            public static byte[] FaultFlag = new byte[1];
            public static int TransXIndex = 0;
            public static int FaultDataXIndex = 0;
            public static int ReceivePacketCounter = 0;
            public static int FaultdataIndexRead = 0;
            public static int i = 0;
            public static int j = 0;
            public static float[] testCsv = new float[50];

        }

        public static float[] floatArray1;
        public static byte[] byteArray;
        public static TimerCallback callback;
        public static Timer stateTimer;
        public static NetworkStream stream;

        public static UInt32 OrdeingIndex;

        public static float[] OrderHelper = new float[RAM_BUFFER_SIZE];
        public static float ErrorIndex;
        public static float RecordCnt;
        public static float FaultRecordCnt;
        public static float RecordBufferSize;
        public static float RecordBufferIndexSizeBeforeError;
        public static float RecordBufferIndexSizeAfterError;
        public static float IndexPerCycle = 6;
        public static void OderingTheRecords()
        {
            ErrorIndex = Globals.transX[RAM_BUFFER_SIZE -60+ 7 /*727*/];
            RecordCnt = Globals.transX[RAM_BUFFER_SIZE - 60 + 8 /*728*/];
            FaultRecordCnt = Globals.transX[RAM_BUFFER_SIZE - 60 + 9 /*729*/];
            RecordBufferSize = Globals.transX[RAM_BUFFER_SIZE - 60 + 10 /*730*/];
            RecordBufferIndexSizeBeforeError = Globals.transX[RAM_BUFFER_SIZE - 60 + 11 /*731*/];
            RecordBufferIndexSizeAfterError = Globals.transX[RAM_BUFFER_SIZE - 60 + 12 /*732*/];

            if (ErrorIndex != 0)
            {
                Console.WriteLine("Ordering and Sorting The Records..");
                for (OrdeingIndex = 0; OrdeingIndex < (ErrorIndex * IndexPerCycle); OrdeingIndex++)
                {
                    OrderHelper[OrdeingIndex] = Globals.transX[OrdeingIndex];
                }
                for (OrdeingIndex = (UInt32)(ErrorIndex * IndexPerCycle); OrdeingIndex < RecordBufferIndexSizeBeforeError; OrdeingIndex++)
                {
                    Globals.transX[Globals.TransXIndex++] = Globals.transX[OrdeingIndex];
                }
                for (OrdeingIndex = 0; OrdeingIndex < (ErrorIndex * IndexPerCycle); OrdeingIndex++)
                {
                    Globals.transX[Globals.TransXIndex++] = OrderHelper[OrdeingIndex];
                }
                Console.WriteLine("Ordering is Finished");
            }

        }

        public static void RecordingToCSV()
        {

            Console.WriteLine("CVS File is Creating..");
            var csv = new StringBuilder();
            var header = string.Format("CIM1_CHANNEL1, CIM1_CHANNEL2, CIM1_CHANNEL3, CIM1_CHANNEL4, CIM1_CHANNEL5, CIM1_CHANNEL6");
            csv.AppendLine(header);

            for (int i = 0; i < RAM_BUFFER_SIZE/SizePerCycle/*140*//*340400*/; i++)
            {
                var newLine = string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\"", Globals.transX[Globals.j++], Globals.transX[Globals.j++], Globals.transX[Globals.j++], Globals.transX[Globals.j++], Globals.transX[Globals.j++], Globals.transX[Globals.j++], Environment.NewLine);
                csv.AppendLine(newLine);
            }
            string filename = "RECORD";
            string baseFileName = "RECORD";
            string cvspath = @"C:\Users\emre.ozdemir\Desktop\TFR Helpers\Records\" + filename + ".csv";
            while (File.Exists(cvspath))
            {
                Globals.i++;
                filename = baseFileName + Globals.i.ToString();
                cvspath = @"C:\Users\emre.ozdemir\Desktop\TFR Helpers\Records\" + filename + ".csv";
            }
            Console.WriteLine("CVS File was Created !!");
            Console.WriteLine("The Fault Record is saved under this directory --->" + cvspath);

            File.AppendAllText(cvspath, csv.ToString());
        }

        static void Main(string[] args)
        {

            // CreateCSV_FromFaultRecords();

            floatArray1 = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            byteArray = new byte[floatArray1.Length * 4];

            Buffer.BlockCopy(floatArray1, 0, byteArray, 0, byteArray.Length);
            var floatArray2 = new float[byteArray.Length / 4];
            Buffer.BlockCopy(byteArray, 0, floatArray2, 0, byteArray.Length);

            Console.WriteLine(floatArray1.SequenceEqual(floatArray2));

            TcpListener listener = new TcpListener(System.Net.IPAddress.Any, 31);
            listener.Start();
            Console.WriteLine("Waiting for the MCM Module TCP connection");
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("MCM accepted");
            Globals.MCM_Connected = true;
            stream = client.GetStream();
            StreamReader streamReader = new StreamReader(client.GetStream());
            StreamWriter streamWriter = new StreamWriter(client.GetStream());

            Console.WriteLine("TFR is initializing -- Sending Configuration Variables");
            stream.Write(Globals.CommanUI, 0, 20);

            //stream.Write(byteArray, 0, 20);

            callback = new TimerCallback(Tick);
            stateTimer = new Timer(callback, null, 0, 15);


            Console.WriteLine("-No Fault-");
            while (true)
            {
                if (Globals.CsvFlag == true)
                {
                    OderingTheRecords();
                    RecordingToCSV();
                    Globals.CsvFlag = false;
                }


            }

        }
        static public void Tick(Object stateInfo)
        {

            if (Globals.MCM_Connected == true && Globals.RecordGetFlag == true)
            {
                if (Globals.TransXIndex != 0)
                {
                    if (Globals.TimeShow == true)
                    {
                        Console.WriteLine("FAULT OCCURED !!! ");
                        Console.WriteLine("Fault Records are uploading: {0} ", DateTime.Now.ToString("h:mm:ss"));
                        Globals.TimeShow = false;
                    }
                }
                Globals.CommanUI[0] = 55;
                stream.Write(Globals.CommanUI, 0, 2);
                stream.Read(Globals.FaultDatas, 0, /*288*/(int)PacketSize*4/*240*/ /*480*/); // !!!!!!!!!!PACKET SIZE!!!!!!!!!!!
                // Globals.TransXIndex =  0;
                Globals.FaultDataXIndex = 0;
                for (int i = 0; i < /*72*/ PacketSize /*60*//*120*/; i++)
                {

                    Globals.transX[Globals.TransXIndex++] = System.BitConverter.ToSingle(Globals.FaultDatas, Globals.FaultDataXIndex);
                    Globals.FaultDataXIndex += 4;
                }

                if (Globals.TransXIndex == RAM_BUFFER_SIZE /*840*/ /*864*/) // !!!!!!!!!!!!!! TOTAL INDEX !!!!!!!!!!!!!!
                {
                    Globals.TransXIndex = 0;
                    Console.WriteLine("All Records Uploaded Succesfully: {0} ", DateTime.Now.ToString("h:mm:ss"));

                    Globals.RecordGetFlag = false;
                    Globals.CsvFlag = true;
                    stateTimer.Change(Timeout.Infinite, Timeout.Infinite); // Disabling the timer and its call back.(Reinit to get the new record).
                }

            }



        }

        /*
      public static void CreateCSV_FromFaultRecords()
      {
          for(int i = 0; i < 50; i++)
          {
              Globals.testCsv[i] = i;
          }
          var csv = new StringBuilder();
          var header = string.Format("CIM1_CHANNEL1, CIM1_CHANNEL2, CIM1_CHANNEL3, CIM1_CHANNEL4, CIM1_CHANNEL5, CIM1_CHANNEL6");
          csv.AppendLine(header);

          for (int i= 0; i<50; i++)
          {
              var newLine = string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\"", Globals.testCsv[i], Globals.testCsv[i], Globals.testCsv[i], Globals.testCsv[i], Globals.testCsv[i], Globals.testCsv[i], Environment.NewLine);
              csv.AppendLine(newLine);
          }
          string filename = "RECORD";
          string baseFileName = "RECORD";
          string cvspath = @"C:\Users\emre.ozdemir\Desktop\TFR Helpers\Records\"+filename+".csv";
          while (File.Exists(cvspath))
          {
              Globals.i++;
              filename = baseFileName + Globals.i.ToString();
              cvspath = @"C:\Users\emre.ozdemir\Desktop\TFR Helpers\Records\" + filename + ".csv";
          }

          File.AppendAllText(cvspath, csv.ToString());
      }*/


    }

}
