using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets.Icmp;
using System.Threading;
using System.Diagnostics;


namespace TraceRouteWindowsApp
{
    public partial class Form1 : Form
    {
        static Stopwatch sw = new Stopwatch();
        static MacAddress sourceMAC;
        static string sourceIP_str;
        static LivePacketDevice selectedDevice;
        static IpV4Address sourceIP;
        static byte ttl_;
        static IpV4Address destinationIP;
        static int count = 50;
        static List<IpV4Address> source_list = new List<IpV4Address>();
        static List <string> pingvalue = new List<string>();
        public Form1()
        {
            InitializeComponent();            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            sourceIP_str = null;
            //****************************
            selectedDevice = allDevices[7];
            //*****************************
            sourceMAC = selectedDevice.GetMacAddress();
            foreach (DeviceAddress address in selectedDevice.Addresses)
            {
                if (address.Address.Family == SocketAddressFamily.Internet)
                    sourceIP_str = address.Address.ToString().Substring(9, address.Address.ToString().Length - 9);
            }
            sourceIP = new IpV4Address(sourceIP_str);
            
        }
        private static void icmp_send()
        {
            using (PacketCommunicator communicator = selectedDevice.Open(100, // name of the device
                                                                         PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                                                         1000)) // read timeout
            {
                
                IcmpEchoLayer icmpLayer = new IcmpEchoLayer();
                for (int i = 1; i < count; i++)
                {
                    Thread.Sleep(100);                    
                    sw.Start();
                    ttl_ = ((byte)i);
                    communicator.SendPacket(BuildIcmpPacket());
                    
                }

            }

        }
        public static void AddItemToListBox(ListBox listBox, string text)
        {
            // Check if the listbox need to be invoked.
            if (listBox.InvokeRequired)
                // Invoke the listbox control with the appropiate delegate.
                listBox.Invoke(new Action<ListBox, string>(AddItemToListBox), listBox, text);
            else
            {
                listBox.Items.Add(text);    
                
            }
        }
        private static Packet BuildIcmpPacket()
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = sourceMAC,
                    Destination = new MacAddress("8C:5A:C1:15:10:92"),
                    EtherType = EthernetType.None, // Will be filled automatically.
                };

            IpV4Layer ipV4Layer =
                new IpV4Layer
                {
                    Source = sourceIP,
                    CurrentDestination = destinationIP,
                    Fragmentation = IpV4Fragmentation.None,
                    HeaderChecksum = null, // Will be filled automatically.
                    Identification = 123,
                    Options = IpV4Options.None,
                    Protocol = null, // Will be filled automatically.
                    Ttl = ttl_,
                    TypeOfService = 0,
                };

            IcmpEchoLayer icmpLayer =
                new IcmpEchoLayer
                {
                    Checksum = null, // Will be filled automatically.
                    Identifier = 456,
                    SequenceNumber = 800,
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, ipV4Layer, icmpLayer);

            return builder.Build(DateTime.Now);
        }
        private static void PacketHandler(Packet packet)
        {
            //Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" + packet.Length+"*******"+packet.IpV4.ToString());
        }
        void Dinle()
        {
            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,                                  
                                                                            
                                    PacketDeviceOpenAttributes.Promiscuous, 
                                    1000))                                  
            {

                using (BerkeleyPacketFilter filter = communicator.CreateFilter("icmp && dst host " + "192.168.1.102"))
                {
                    
                    communicator.SetFilter(filter);
                }
                string mesaj2;
                Packet p;
                do
                {
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out p); 
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            if (ttl_ < count - 1)
                                AddItemToListBox(listBox, "Time out");
                            continue;
                        case PacketCommunicatorReceiveResult.Ok: //id icmp layerdedir. 

                            IpV4Datagram ip = p.Ethernet.IpV4;

                            IpV4Address dst = ip.Destination, src = ip.Source;
                            int x = (int)ip.Ttl;                            

                            AddItemToListBox(listBox, $"Ip:{src}->TTL:{x}");
                            //AddItemToListBox(listBox, x.ToString());
                            Console.WriteLine("ttl status:" + x);
                            source_list.Add(src);
                            //source_list.Distinct().ToList();
                            Console.WriteLine("Source:" + source_list.Last());
                            sw.Stop();
                            string elapsed1= sw.ElapsedMilliseconds.ToString();                        
                            break;
                        default:
                            throw new InvalidOperationException("The result " + result + " should never be reached here");
                    }
                } while (source_list.Last().ToString() != destinationIP.ToString());//sonsuza kadar dinle.
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            source_list.Clear();
            
            //******************************************
            Thread thread2 = new Thread(icmp_send);
            Thread thread1 = new Thread(Dinle);
            
            try
            {
                destinationIP = new IpV4Address(textBox1.Text.ToString());
                thread1.Start();
                thread2.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            } 

        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            listBox.Items.Clear();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            
        }
    }
}
