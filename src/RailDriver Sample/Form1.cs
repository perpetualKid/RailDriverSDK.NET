using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RailDriver.Sample
{
    public partial class Form1 : Form, IDataHandler, IErrorHandler
    {
        private IList<PIEDevice> devices;
        private int[] cbotodevice = null; //for each item in the CboDevice list maps this index to the device index.  Max devices =100 
        private byte[] wData = null; //write data buffer
        private int selecteddevice = -1; //set to the index of CboDevice

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
            cbotodevice = new int[128]; //128=max # of devices
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
                    if (devices[i].HidUsagePage == 0xc)
                    {
                        switch (devices[i].Pid)
                        {
                            case 210:
                                CboDevices.Items.Add("RailDriver (" + devices[i].Pid + "), ID: " + i);
                                cbotodevice[cbocount] = i;
                                cbocount++;
                                break;

                            default:
                                CboDevices.Items.Add("Unknown Device (" + devices[i].Pid + "), ID: " + i);
                                cbotodevice[cbocount] = i;
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
                selecteddevice = cbotodevice[CboDevices.SelectedIndex];
                wData = new byte[devices[selecteddevice].WriteLength];//go ahead and setup for write
            }

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //closeinterfaces on all devices
            for (int i = 0; i < CboDevices.Items.Count; i++)
            {
                devices[cbotodevice[i]].CloseInterface();
            }
            System.Environment.Exit(0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }

        private void CboDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            selecteddevice = cbotodevice[CboDevices.SelectedIndex];
            wData = new byte[devices[selecteddevice].WriteLength];//size write array 
        }

        private void BtnCallback_Click(object sender, EventArgs e)
        {
            //setup callback if there are devices found for each device found

            if (CboDevices.SelectedIndex != -1)
            {
                for (int i = 0; i < CboDevices.Items.Count; i++)
                {
                    //use the cbotodevice array which contains the mapping of the devices in the CboDevices to the actual device IDs
                    devices[cbotodevice[i]].SetErrorCallback(this);
                    devices[cbotodevice[i]].SetDataCallback(this);
                    devices[cbotodevice[i]].SuppressDuplicateReports = true;
                    devices[cbotodevice[i]].CallNever = false;
                }

            }
        }

        //data callback    
        public void HandleHidData(byte[] data, PIEDevice sourceDevice, int error)
        {
            //check the sourceDevice and make sure it is the same device as selected in CboDevice   
            if (sourceDevice == devices[selecteddevice])
            {

                //write raw data to listbox1
                string output = "Callback: " + sourceDevice.Pid + ", ID: " + selecteddevice.ToString() + ", data=";
                for (int i = 0; i < sourceDevice.ReadLength; i++)
                {
                    output = output + data[i].ToString() + "  ";
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
            SetToolStrip("Error: " + error.ToString());
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
            if (CboDevices.SelectedIndex != -1)
            {
                //write to the LED Segments
                for (int j = 0; j < devices[selecteddevice].WriteLength; j++)
                {
                    wData[j] = 0;
                }

                wData[1] = 134;
                wData[2] = byte.Parse(textBox1.Text);
                wData[3] = byte.Parse(textBox2.Text);
                wData[4] = byte.Parse(textBox3.Text);
                int result = 404;
                while (result == 404) { result = devices[selecteddevice].WriteData(wData); }
                if (result != 0)
                {
                    toolStripStatusLabel1.Text = "Write Fail: " + result;
                }
                else
                {
                    toolStripStatusLabel1.Text = "Write Success - Write to Display";
                }
            }
        }

        private void BtnSpeakerOn_Click(object sender, EventArgs e)
        {
            if (CboDevices.SelectedIndex != -1)
            {
                //turn speaker on
                //the following will turn on the RailDriver speaker and
                //also enable Timer1 which plays a sound.  If you wish to
                //hear only the RailDriver speaker disconnect the Speaker
                //Pass Thru in the back of the unit.  Make sure the RailDriver
                //power and speaker are plugged in.
                for (int j = 0; j < devices[selecteddevice].WriteLength; j++)
                {
                    wData[j] = 0;
                }

                wData[1] = 133;
                wData[7] = 1;

                int result = 404;
                while (result == 404) { result = devices[selecteddevice].WriteData(wData); }
                if (result != 0)
                {
                    toolStripStatusLabel1.Text = "Write Fail: " + result;
                }
                else
                {
                    toolStripStatusLabel1.Text = "Write Success - Speaker On";
                    timer1.Enabled = true;
                }
            }
        }

        private void BtnSpeakerOff_Click(object sender, EventArgs e)
        {
            if (CboDevices.SelectedIndex != -1)
            {
                //turn speaker off

                for (int j = 0; j < devices[selecteddevice].WriteLength; j++)
                {
                    wData[j] = 0;
                }

                wData[1] = 133;
                wData[7] = 0;

                int result = 404;
                while (result == 404) { result = devices[selecteddevice].WriteData(wData); }
                if (result != 0)
                {
                    toolStripStatusLabel1.Text = "Write Fail: " + result;
                }
                else
                {
                    toolStripStatusLabel1.Text = "Write Success - Speaker Off";
                    timer1.Enabled = false;
                }
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            System.Media.SystemSounds.Beep.Play();
        }
    }
}
