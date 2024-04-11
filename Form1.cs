using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Chair_TCP
{
    public partial class Form1 : Form
    {
        private TcpListener myListener;
        private TcpClient client;
        private BinaryWriter bw;
        private Thread myThread;
        private Chair_value processedData; // 存储从 ProcessData 获取的数据
        private int dataIndex = 0, xIndex = 0; // 跟踪当前更新到的数据点索引
        private static System.Threading.Timer myTimer;
        private const int MaxPoints = 100;  // 设置最大数据点数
        private string extension, filePath;
        private int[] offset = { 0, 0, 0, 0 };
        private byte cnt = 0;
        private bool cnt_flag = false, Check = false;
        private int TCP_LEN = 960;


        public Form1()
        {
            InitializeComponent();
            // 添加FormClosing事件处理程序
            this.FormClosing += Form1_FormClosing;
            chart1.Series[0].Color = Color.Blue;
            chart2.Series[0].Color = Color.Red;
            chart3.Series[0].Color = Color.Green;
            chart4.Series[0].Color = Color.Orange;
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0";
            chart2.ChartAreas[0].AxisY.LabelStyle.Format = "0";
            chart3.ChartAreas[0].AxisY.LabelStyle.Format = "0";
            chart4.ChartAreas[0].AxisY.LabelStyle.Format = "0";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 断开网络连接
            Disconnect();
        }

        private void Disconnect()
        {
            if (myTimer != null)
            {
                myTimer.Change(Timeout.Infinite, Timeout.Infinite);
                myTimer.Dispose();
                myTimer = null;
            }
            // 检查客户端是否已连接，然后关闭它
            if (client != null && client.Connected)
            {
                client.Close();
            }

            // 停止监听
            if (myListener != null)
            {
                myListener.Stop();
            }
            if (myThread != null)
            {
                myThread.Abort();
            }



        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (myThread != null && myThread.IsAlive)
            {
                myThread.Abort();
                myThread = null;
            }
            myThread = new Thread(ServerA);
            myThread.Start();
            button1.Enabled = false;
        }

        private void ServerA()
        {
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addr = ipEntry.AddressList;
            //IPAddress ip = addr[0];
            try
            {
                IPAddress ip = IPAddress.Parse("10.0.0.2");
                myListener = new TcpListener(ip, 1234);
                myListener.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("檢查wifi是否正確連線：" + ex.Message);
                myListener?.Stop();
                this.Invoke((MethodInvoker)delegate
                {
                    button1.Enabled = true;
                    button4.Enabled = false;
                });
                return;
            }

            // 在UI上顯示等待連接的訊息
            this.Invoke((MethodInvoker)delegate
            {
                label1.Text = "Waiting for connection...";
            });

            while (true)
            {
                if (!myListener.Pending())
                {
                    //为了避免每次都被tcpListener.AcceptTcpClient()阻塞线程，添加了此判断，
                    //no connection requests have arrived。
                    //当没有连接请求时，什么也不做，有了请求再执行到tcpListener.AcceptTcpClient()
                }
                else
                {
                    client = myListener.AcceptTcpClient();


                    // 連接成功後，在UI上顯示連接成功的訊息
                    this.Invoke((MethodInvoker)delegate
                    {
                        label1.Text = "Connected";
                        button2.Enabled = true;
                        button4.Enabled = true;
                    });

                    break;
                }
            }

            NetworkStream stream = client.GetStream();
            bw = new BinaryWriter(stream);

            byte[] buffer = new byte[TCP_LEN + 1]; // 创建一个足够大的缓冲区来存储481个字节

            try
            {
                while (client.Connected)
                {
                    int bytesRead = 0;
                    if (timer1.Enabled == false)
                    {
                        Check = true;
                        this.Invoke((MethodInvoker)delegate
                        {
                            timer1.Start();
                        });
                    }
                    while (bytesRead < buffer.Length)
                    {
                        int read = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
                        if (read == 0)
                        {
                            // 连接已关闭或结束
                            break;
                        }
                        bytesRead += read;
                    }

                    if (bytesRead == buffer.Length)
                    {
                        Check = true;
                        processedData = ProcessData(buffer);
                        dataIndex = 0;
                        // 此处处理接收到的240个字节
                        this.Invoke((MethodInvoker)delegate
                        {
                            // 定义回调方法
                            TimerCallback timerCallback = new TimerCallback(TimerTick);

                            // 创建定时器，设置为1毫秒（10000 ticks = 1毫秒）
                            myTimer = new System.Threading.Timer(timerCallback, null, 0, 10);
                            //textBox1.AppendText("Received 240 bytes" + Environment.NewLine);

                            if (checkBox1.Checked)
                            {
                                // 根据扩展名决定如何保存数据
                                if (extension == ".txt" || extension == ".log")
                                {
                                    // 为文本文件追加文本数据
                                    using (StreamWriter writer = new StreamWriter(filePath, true)) // true 表示追加数据
                                    {
                                        for (int i = 0; i < TCP_LEN/12; i++)
                                            writer.WriteLine(processedData.CH1[i].ToString("D8") + "\t" + processedData.CH2[i].ToString("D8") + "\t" + processedData.CH3[i].ToString("D8") + "\t" + processedData.CH4[i].ToString("D8") + "\t" + cnt.ToString()); // WriteLine 会在 textData 后添加换行符
                                    }

                                }
                                else if (extension == ".bin" || extension == ".dat")
                                {
                                    /// 为二进制文件追加二进制数据
                                    using (FileStream fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
                                    {
                                        for (int i = 0; i < processedData.CH1.Length; i++)
                                        {
                                            // 将 CH1 到 CH4 的每个值转换为字节序列并写入文件
                                            fileStream.Write(BitConverter.GetBytes(processedData.CH1[i]), 0, sizeof(int));
                                            fileStream.Write(BitConverter.GetBytes(processedData.CH2[i]), 0, sizeof(int));
                                            fileStream.Write(BitConverter.GetBytes(processedData.CH3[i]), 0, sizeof(int));
                                            fileStream.Write(BitConverter.GetBytes(processedData.CH4[i]), 0, sizeof(int));
                                        }
                                    }
                                }
                                else
                                {
                                    // 未知或不支持的文件类型，根据需要处理
                                    //Console.WriteLine("不支持的文件类型：" + extension);
                                }
                            }

                        });
                    }
                }
            }
            catch (SocketException ex)
            {
                // SocketException 提供了 ErrorCode 属性来标识具体的错误
                if (ex.ErrorCode == 10054) // 10054 是连接被对方重置的错误代码
                {
                    Console.WriteLine("网络连接被对方重置，可能是网络中断或对方崩溃。");
                }
                else
                {
                    Console.WriteLine($"Socket 错误 {ex.ErrorCode}: {ex.Message}");
                }
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    // IOException 有时候会包裹 SocketException
                    if (socketEx.ErrorCode == 10054)
                    {
                        Console.WriteLine("網路連接重製，可能是網路中斷或崩潰。");
                    }
                    else if (socketEx.ErrorCode == 10053)
                    {
                        MessageBox.Show("WIFI斷線：" + ex.Message);
                        this.Invoke((MethodInvoker)delegate
                        {
                            button1.Enabled = true;
                            button4.Enabled = false;
                            button2.Enabled = false;
                        });
                        if (myListener != null)
                        {
                            myListener.Stop();
                        }
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"内部 Socket 错误 {socketEx.ErrorCode}: {socketEx.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("I/O 错误: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("發生錯誤：" + ex.Message);
            }
        }


        public class Chair_value
        {
            public int[] CH1 { get; set; }
            public int[] CH2 { get; set; }
            public int[] CH3 { get; set; }
            public int[] CH4 { get; set; }

            public Chair_value(int length)
            {
                CH1 = new int[length];
                CH2 = new int[length];
                CH3 = new int[length];
                CH4 = new int[length];
            }
        }


        public Chair_value ProcessData(byte[] buffer)
        {
            if (buffer == null || (buffer.Length - 1) % 12 != 0)
            {
                throw new ArgumentException("Buffer length must be a multiple of 12");
            }

            int length = buffer.Length / 12; // 每12个字节代表四个通道的一次数据
            Chair_value result = new Chair_value(length);

            for (int i = 0, j = 0; i < buffer.Length - 1; i += 12, j++)
            {
                // 每次处理12个字节，分别代表4个通道的3个字节
                result.CH1[j] = ((buffer[i] << 24) | (buffer[i + 1] << 16) | (buffer[i + 2] << 8)) >> 8;
                result.CH2[j] = ((buffer[i + 3] << 24) | (buffer[i + 4] << 16) | (buffer[i + 5] << 8)) >> 8;
                result.CH3[j] = ((buffer[i + 6] << 24) | (buffer[i + 7] << 16) | (buffer[i + 8] << 8)) >> 8;
                result.CH4[j] = ((buffer[i + 9] << 24) | (buffer[i + 10] << 16) | (buffer[i + 11] << 8)) >> 8;

                result.CH1[j] -= offset[0];
                result.CH2[j] -= offset[1];
                result.CH3[j] -= offset[2];
                result.CH4[j] -= offset[3];
            }
            /*if (!cnt_flag)
            {
                cnt_flag = true;
            }
            else
            {
                if ((buffer[buffer.Length - 1] - cnt == 1) || (buffer[buffer.Length - 1] - cnt == -255))
                {

                }
                else
                {
                    checkBox1.Checked = false;
                    cnt_flag = false;
                    button4_Click(null, EventArgs.Empty);
                    MessageBox.Show("Data Loss! Sampling Stopped");
                }
            }*/
            cnt = buffer[buffer.Length - 1];
            return result;
        }

        private void TimerTick(object state)
        {
            // 使用 Invoke 确保在 UI 线程上执行更新操作
            this.Invoke((MethodInvoker)delegate
            {
                
                // 更新图表的每个通道
                UpdateChart(chart1, xIndex, processedData.CH1[dataIndex]);
                UpdateChart(chart2, xIndex, processedData.CH2[dataIndex]);
                UpdateChart(chart3, xIndex, processedData.CH3[dataIndex]);
                UpdateChart(chart4, xIndex, processedData.CH4[dataIndex]);


                dataIndex += 10; // 移动到下一个数据点
                xIndex += 10;

                if (dataIndex >= 79)
                {
                    dataIndex = 0;
                    myTimer.Change(Timeout.Infinite, Timeout.Infinite); // 停止定时器
                }
            });
        }


        private void SendData(TcpClient client, string message)
        {
            // 获取网络流
            NetworkStream stream = client.GetStream();

            // 将消息转换为字节
            byte[] data = Encoding.UTF8.GetBytes(message);

            // 发送数据到客户端
            stream.Write(data, 0, data.Length);

            Console.WriteLine("Data sent to client.");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 4; i++)
            {
                offset[i] = 0;
            }

            for (int i = 0; i < processedData.CH1.Length; i++)
            {
                offset[0] += processedData.CH1[i];
            }
            offset[0] /= processedData.CH1.Length;

            for (int i = 0; i < processedData.CH2.Length; i++)
            {
                offset[1] += processedData.CH2[i];
            }
            offset[1] /= processedData.CH2.Length;

            for (int i = 0; i < processedData.CH3.Length; i++)
            {
                offset[2] += processedData.CH3[i];
            }
            offset[2] /= processedData.CH3.Length;

            for (int i = 0; i < processedData.CH4.Length; i++)
            {
                offset[3] += processedData.CH4[i];
            }
            offset[3] /= processedData.CH4.Length;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            label1.Text = "Disconnected";
            timer1.Stop();
            SendData(client, "STOP");
            if (myTimer != null)
            {
                myTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (myListener != null)
            {
                myListener.Stop();
            }
            if (client != null && client.Connected)
            {
                client.Close();
            }
            button1.Enabled = true;
            button4.Enabled = false;
            button2.Enabled = false;
            return;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if(!Check)
            {
                checkBox1.Checked = false;
                timer1.Stop();
                button4_Click(null, EventArgs.Empty);
                MessageBox.Show("Connect Timeout! Sampling Stopped");
            }
            else
            {
                Check = false;
            }
        }

        private void UpdateChart(Chart chart, double xValue, double yValue)
        {
            // 添加新的数据点
            chart.Series[0].Points.AddXY(xValue, yValue);
            int max = (int)chart.Series[0].Points.FindMaxByValue().YValues[0];
            int min = (int)chart.Series[0].Points.FindMinByValue().YValues[0];
            if (chart == chart1)
            {
                label2.Text = yValue.ToString();
            }
            else if (chart == chart2)
            {
                label3.Text = yValue.ToString();
            }
            else if (chart == chart3)
            {
                label4.Text = yValue.ToString();
            }
            else if (chart == chart4)
            {
                label5.Text = yValue.ToString();
            }
            // 检查数据点数是否超过最大值
            if (chart.Series[0].Points.Count > MaxPoints)
            {
                // 移除最早的数据点以保持图表中的点数为1000
                chart.Series[0].Points.RemoveAt(0);


                chart.ChartAreas[0].AxisX.Minimum = xValue - MaxPoints * 10;
                chart.ChartAreas[0].AxisX.Maximum = xValue;

                chart.ChartAreas[0].AxisY.Minimum = min - 2500;
                chart.ChartAreas[0].AxisY.Maximum = max + 2500;
            }
            chart.ChartAreas[0].AxisY.Minimum = min - 2500;
            chart.ChartAreas[0].AxisY.Maximum = max + 2500;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Select Save Path";
            dialog.Filter = "Text files (*.txt)|*.txt|Log files(*.log)|*.log|binary files(*.bin)|*.bin|All files(*.bin;*.txt;*.log)|*.bin;*.txt;*log";
            //这是系统提供的桌面路径，还可以是其他的路径：比如文档、音乐等文件夹
            dialog.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (dialog.ShowDialog() == DialogResult.OK)   // 窗体打开成功
            {
                filePath = dialog.FileName;  // 获取文件的路径
                textBox3.Text = filePath;
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close();
                }
                if (textBox3.Text != "")
                    checkBox1.Enabled = true;
                //MessageBox.Show("已选择文件夹:" + filePath, "选择文件夹提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                extension = Path.GetExtension(filePath).ToLower();
            }
        }
    }
}
