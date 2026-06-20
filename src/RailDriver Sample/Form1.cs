using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RailDriver.Sample
{
    internal sealed partial class Form1 : Form, IDataHandler, IErrorHandler
    {
        private const int Success = 0;
        private const int WriteBufferFull = 404;
        private const int MaxWriteRetries = 10;
        private const int MaxDevices = 128;
        private const int RailDriverHidUsagePage = 0x0c;
        private const int RailDriverProductId = 210;

        private IList<PIEDevice> devices;
        private int[] comboToDevice; //maps each CboDevices entry to the enumerated device index
        private byte[] writeData; //write data buffer
        private int selectedDevice = -1; //index into CboDevices

        //for thread-safe way to call a Windows Forms control
        // This delegate enables asynchronous calls for setting
        // the text property on a TextBox control.
        private delegate void SetTextCallback(string text);
        //end thread-safe

        private readonly SetTextCallback setListBoxTextDelegate;
        private readonly SetTextCallback setToolStripTextDelegate;

        public Form1()
        {
            InitializeComponent();
            setListBoxTextDelegate = new SetTextCallback(SetListBox);
            setToolStripTextDelegate = new SetTextCallback(SetToolStrip);
        }

        private void BtnEnumerate_Click(object sender, EventArgs e)
        {
            CboDevices.Items.Clear();
            comboToDevice = new int[MaxDevices];
            //enumerate and setupinterfaces for all devices
            devices = PIEDevice.EnumeratePIE();
            if (devices.Count == 0)
            {
                toolStripStatusLabel1.Text = "No Devices Found";
            }
            else
            {
                //System.Media.SystemSounds.Beep.Play(); 
                int cbocount = 0; //keeps track of how many valid devices were added to the CboDevice box
                for (int i = 0; i < devices.Count; i++)
                {
                    //information about device
                    //PID = devices[i].Pid);
                    //HID Usage = devices[i].HidUsage);
                    //HID Usage Page = devices[i].HidUsagePage);
                    //HID Version = devices[i].Version);
                    if (devices[i].HidUsagePage == RailDriverHidUsagePage)
                    {
                        switch (devices[i].Pid)
                        {
                            case RailDriverProductId:
                                CboDevices.Items.Add("RailDriver (" + devices[i].Pid + "), ID: " + i);
                                comboToDevice[cbocount] = i;
                                cbocount++;
                                break;

                            default:
                                CboDevices.Items.Add("Unknown Device (" + devices[i].Pid + "), ID: " + i);
                                comboToDevice[cbocount] = i;
                                cbocount++;
                                break;
                        }
                        devices[i].SetupInterface();
                    }
                }
            }
            if (CboDevices.Items.Count > 0)
            {
                CboDevices.SelectedIndex = 0;
                selectedDevice = comboToDevice[CboDevices.SelectedIndex];
                writeData = new byte[devices[selectedDevice].WriteLength];//go ahead and setup for write
            }

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (devices == null || comboToDevice == null)
                return;

            //closeinterfaces on all devices
            for (int i = 0; i < CboDevices.Items.Count; i++)
            {
                devices[comboToDevice[i]].CloseInterface();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }

        private void CboDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedDevice = comboToDevice[CboDevices.SelectedIndex];
            writeData = new byte[devices[selectedDevice].WriteLength];//size write array 
        }

        private void BtnCallback_Click(object sender, EventArgs e)
        {
            //setup callback if there are devices found for each device found

            if (CboDevices.SelectedIndex != -1)
            {
                for (int i = 0; i < CboDevices.Items.Count; i++)
                {
                    //use the comboToDevice array which maps CboDevices entries to actual device IDs
                    devices[comboToDevice[i]].SetErrorCallback(this);
                    devices[comboToDevice[i]].SetDataCallback(this);
                    devices[comboToDevice[i]].SuppressDuplicateReports = true;
                    devices[comboToDevice[i]].CallNever = false;
                }

            }
        }

        //data callback    
        public void HandleHidData(byte[] data, PIEDevice sourceDevice, int error)
        {
            //check the sourceDevice and make sure it is the same device as selected in CboDevice   
            if (sourceDevice == devices[selectedDevice])
            {
                //write raw data to listbox1
                string output = $"Callback: {sourceDevice.Pid}, ID: {selectedDevice}, data=";
                for (int i = 0; i < sourceDevice.ReadLength; i++)
                {
                    output += $"{data[i]}  ";
                }
                //Reverser = rdata[1]
                //Throttle = rdata[2]
                //AutoBrake = rdata[3]
                //Ind Brake = rdata[4]
                //Bail Off = rdata[5]
                //Wiper = rdata[6]
                //Lights = rdata[7]
                //buttons = rdata[8] to rdata[13]
                SetListBox(output);
            }

        }

        //error callback
        public void HandleHidError(PIEDevice sourceDevice, int error)
        {
            SetToolStrip($"Error: {error}");
        }

        //for threadsafe setting of Windows Forms control
        private void SetListBox(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(setListBoxTextDelegate, text);
            }
            else
            {
                listBox1.Items.Add(text);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            }
        }

        //for threadsafe setting of Windows Forms control
        private void SetToolStrip(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (statusStrip1.InvokeRequired)
            {
                statusStrip1.Invoke(setToolStripTextDelegate, text);
            }
            else
            {
                toolStripStatusLabel1.Text = text;
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void BtnWriteDisplay_Click(object sender, EventArgs e)
        {
            if (CanWriteToSelectedDevice())
            {
                //write to the LED Segments
                ClearWriteData();

                writeData[1] = 134;
                if (byte.TryParse(textBox1.Text, out writeData[2]) && byte.TryParse(textBox2.Text, out writeData[3]) && byte.TryParse(textBox3.Text, out writeData[4]))
                {
                    SetWriteStatus(WriteSelectedDevice(), "Write Success - Write to Display");
                }
                else 
                {
                    toolStripStatusLabel1.Text = "Input Error - data out of valid range";
                }
            }
        }

        private void BtnSpeakerOn_Click(object sender, EventArgs e)
        {
            if (CanWriteToSelectedDevice())
            {
                //turn speaker on
                //the following will turn on the RailDriver speaker and
                //also enable Timer1 which plays a sound.  If you wish to
                //hear only the RailDriver speaker disconnect the Speaker
                //Pass Thru in the back of the unit.  Make sure the RailDriver
                //power and speaker are plugged in.
                ClearWriteData();

                writeData[1] = 133;
                writeData[7] = 1;

                if (SetWriteStatus(WriteSelectedDevice(), "Write Success - Speaker On"))
                {
                    timer1.Enabled = true;
                }
            }
        }

        private void BtnSpeakerOff_Click(object sender, EventArgs e)
        {
            if (CanWriteToSelectedDevice())
            {
                //turn speaker off

                ClearWriteData();

                writeData[1] = 133;
                writeData[7] = 0;

                if (SetWriteStatus(WriteSelectedDevice(), "Write Success - Speaker Off"))
                {
                    timer1.Enabled = false;
                }
            }
        }

        private bool CanWriteToSelectedDevice()
        {
            return CboDevices.SelectedIndex != -1 && devices != null && writeData != null;
        }

        private void ClearWriteData()
        {
            Array.Clear(writeData, 0, writeData.Length);
        }

        private int WriteSelectedDevice()
        {
            int result = WriteBufferFull;
            for (int retry = 0; retry < MaxWriteRetries && result == WriteBufferFull; retry++)
            {
                result = devices[selectedDevice].WriteData(writeData);
            }

            return result;
        }

        private bool SetWriteStatus(int result, string successMessage)
        {
            if (result != Success)
            {
                toolStripStatusLabel1.Text = "Write Fail: " + result;
                return false;
            }

            toolStripStatusLabel1.Text = successMessage;
            return true;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            System.Media.SystemSounds.Beep.Play();
        }
    }
}
