using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;

/*
等待添加的功能
1、窗体大小可调，内部控件大小可以随之调节大小
*/

namespace CSDN_COM
{
    public partial class Form1 : Form
    {
        private long receieve_count = 0;
        private long send_count = 0;
        private StringBuilder sb = new StringBuilder();
        private DateTime current_time = new DateTime();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //获取电脑当前可用串口并添加到选项列表
            comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());

            //批量添加波特率列表
            string[] baud = { "4300", "9600", "9600", "38400", "115200", };
            comboBox2.Items.AddRange(baud);

            //设置默认值
            comboBox1.Text = "COM1";
            comboBox2.Text = "115200";
            comboBox3.Text = "8";
            comboBox4.Text = "None";
            comboBox5.Text = "1";

            //初始时的状态栏显示
            label7.Text = "串口已关闭";
            label7.ForeColor = Color.Red;
            label8.Text = "Tx:" + send_count.ToString() + " Bytes";
            label9.Text = "Rx:" + receieve_count.ToString() + " Bytes";
            label10.Text = "V1.0";
            label10.ForeColor = Color.Blue;

            //为了避免出错，启动时发送按钮失能
            button2.Enabled = false;
        }

        //打开串口按钮
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //将可能产生异常的处理代码放在try块中，根据当前串口属性来判断是否打开
                if(serialPort1.IsOpen)
                {
                    //串口已经打开
                    serialPort1.Close();
                    button2.Enabled = false;
                    //更新状态栏
                    label7.Text = "串口已关闭";
                    label7.ForeColor = Color.Red;

                    button1.Text = "打开串口";
                    button1.BackColor = Color.ForestGreen;
                    comboBox1.Enabled = true;
                    comboBox2.Enabled = true;
                    comboBox3.Enabled = true;
                    comboBox4.Enabled = true;
                    comboBox5.Enabled = true;
                    //textBox_R.Text = "";//清空接收区
                    //textBox_T.Text = "";//清空发送区
                }
                else
                {
                    //如果点击按钮时串口是关闭的，说明当前操作是要打开串口
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    comboBox3.Enabled = false;
                    comboBox4.Enabled = false;
                    comboBox5.Enabled = false;
                    //根据设置进行串口配置              
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);
                    serialPort1.DataBits = Convert.ToInt16(comboBox3.Text);

                    if (comboBox4.Text.Equals("None"))
                        serialPort1.Parity = System.IO.Ports.Parity.None;
                    else if(comboBox4.Text.Equals("Odd"))
                        serialPort1.Parity = System.IO.Ports.Parity.Odd;
                    else if (comboBox4.Text.Equals("Even"))
                        serialPort1.Parity = System.IO.Ports.Parity.Even;
                    else if (comboBox4.Text.Equals("Mark"))
                        serialPort1.Parity = System.IO.Ports.Parity.Mark;
                    else if (comboBox4.Text.Equals("Space"))
                        serialPort1.Parity = System.IO.Ports.Parity.Space;

                    if (comboBox5.Text.Equals("1"))
                        serialPort1.StopBits = System.IO.Ports.StopBits.One;
                    else if (comboBox5.Text.Equals("1.5"))
                        serialPort1.StopBits = System.IO.Ports.StopBits.OnePointFive;
                    else if (comboBox5.Text.Equals("2"))
                        serialPort1.StopBits = System.IO.Ports.StopBits.Two;

                    serialPort1.Open();//设置完毕后打开串口
                    //更新状态栏
                    label7.Text = "串口已打开";
                    label7.ForeColor = Color.Green;
                    button1.Text = "关闭串口";
                    button1.BackColor = Color.Firebrick;
                    button2.Enabled = true; //使能发送按钮
                }
            }
            catch(Exception ex)
            {
                //捕获可能发生的异常并进行处理

                //捕获到异常，创建一个新的对象，之前不能在用
                serialPort1 = new System.IO.Ports.SerialPort();

                //刷新COM选项
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());

                //显示异常给用户
                System.Media.SystemSounds.Beep.Play();
                button1.Text = "打开串口";
                button1.BackColor = Color.ForestGreen;
                MessageBox.Show(ex.Message);    //显示异常问题
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
            }
        }

        //发送按钮
        private void button2_Click(object sender, EventArgs e)
        {
            byte[] temp = new byte[1];  //获取发送缓冲区的一个字符

            try
            {
                //串口处于开启状态，发送缓冲区内容
                if (serialPort1.IsOpen)
                {
                    int send_byte_num = 0;//本次发送字节数

                    //判断发送模式
                    if(radioButton3.Checked)
                    {
                        //HEX模式发送
                        //首先用正则表达式将用户输入的16进制字符匹配出来
                        string buf = textBox_T.Text;
                        string patten = @"\s";
                        string replacement = "";
                        Regex rgx = new Regex(patten);
                        string send_data = rgx.Replace(buf, replacement);

                        //发送匹配替换后的字符
                        send_byte_num = (send_data.Length - send_data.Length % 2) / 2;
                        for(int i = 0;i < send_byte_num;i++)
                        {
                            temp[0] = Convert.ToByte(send_data.Substring(i * 2, 2), 16);
                            serialPort1.Write(temp, 0, 1);
                        }
                        //字符数为奇数，单独处理
                        if(send_byte_num % 2 != 0)
                        {
                            temp[0] = Convert.ToByte(send_data.Substring(textBox_T.Text.Length-1,1), 16);
                            serialPort1.Write(temp, 0, 1);
                            send_byte_num++;
                        }
                        if (checkBox4.Checked)
                        {
                            //自动发送新行
                            serialPort1.WriteLine("");
                        }
                    }
                    else
                    {
                        //ASCII模式发送
                        //判断是否发送新行
                        if(checkBox4.Checked)
                        {
                            serialPort1.WriteLine(textBox_T.Text);
                            send_byte_num = textBox_T.Text.Length + 2;  //回车占2个字节
                        }
                        else
                        {
                            //不发送新行
                            serialPort1.Write(textBox_T.Text);
                            send_byte_num = textBox_T.Text.Length;
                        }
                    }
                    send_count += send_byte_num;    //计数变量刷新
                    label8.Text = "Tx:" + send_count.ToString() + "Bytes";  //刷新显示
                }
                
            }
            catch(Exception ex)
            {
                serialPort1.Close();
                //捕获到异常，创建一个新的对象，之前不能在用
                serialPort1 = new System.IO.Ports.SerialPort();

                //刷新COM选项
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());

                //显示异常给用户
                System.Media.SystemSounds.Beep.Play();
                button1.Text = "打开串口";
                button1.BackColor = Color.ForestGreen;
                MessageBox.Show(ex.Message);    //显示异常问题
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
            }
        }

        //串口接受事件处理
        private void SerialPort1_DataReceieved(object sender, SerialDataReceivedEventArgs e)
        {
            //方法2：按字节读取
            int num = serialPort1.BytesToRead;  //获取缓冲区字节数
            byte[] received_buf = new byte[num];//声明一个字节型数组，大小是num个

            receieve_count += num;
            serialPort1.Read(received_buf, 0, num); //将缓冲区数据读取到received_buf

            sb.Clear(); //防止出错，先清空字符串构造器
            
            if(radioButton2.Checked)    //以HEX形式接收
            {
                foreach(byte b in received_buf)
                {
                    sb.Append(b.ToString("X2") + " ");  //byte转化为2位16进制文本进行显示，中间用空格隔开
                }
            }
            else  //默认ascii形式接收
            {
                    sb.Append(Encoding.ASCII.GetString(received_buf));//将接收数组解码为ascii数组
            }
            //显示到接收文本框内
            try
            {
                Invoke((EventHandler)(delegate
                {
                    if(checkBox1.Checked)
                    {
                        //显示时间
                        current_time = System.DateTime.Now;
                        textBox_R.AppendText(current_time.ToString("HH:mm:ss") + " " + sb.ToString());
                    }
                    else
                    {
                        textBox_R.AppendText(sb.ToString());
                    }
                    if (checkBox2.Checked)
                    {
                        //接收自动换行
                        textBox_R.AppendText(Environment.NewLine);
                    }
                    //更新状态栏
                    label9.Text = "Rx:" + receieve_count.ToString() + "Bytes";
                }));
            }
            catch (Exception ex)
            {
                //响铃并显示异常信息
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show(ex.Message);
            }
        }
        
        //清空接收按钮
        private void button3_Click(object sender, EventArgs e)
        {
            textBox_R.Text = "";
            receieve_count = 0;
            label9.Text = "Rx:" + receieve_count.ToString() + "Bytes";
        }

        //清空发送按钮
        private void button4_Click(object sender, EventArgs e)
        {
            textBox_T.Text = "";
            send_count = 0;
            label8.Text = "Tx:" + send_count.ToString() + "Bytes";
        }

        //自动定时发送
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox3.Checked)
            {
                //选择自动发送
                numericUpDown1.Enabled = false;
                timer1.Interval = (int)numericUpDown1.Value;    //定时器赋值，单位：毫秒
                timer1.Start();
                label7.Text = "串口已打开" + "自动发送中...";
            }
            else
            {
                //取消选中，停止自动发送
                numericUpDown1.Enabled = true;
                timer1.Stop();
                label7.Text = "串口已打开";
            }
        }

        //定时时间到
        private void timer1_tick(object sender, EventArgs e)
        {
            button2_Click(button2, new EventArgs());//调用发送按钮的回调函数
        }
    }
}

