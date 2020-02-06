using System;
using System.Net;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HTTPStressTest {
    public partial class Form1 : Form {

        private bool testing = false;
        private Thread thread;
        private int t, tested, success, failed, timeout = 500;
        private Uri uri;
        private string method = "GET";
        private SynchronizationContext syncContext_tested = null;
        private SynchronizationContext syncContext_success = null;
        private SynchronizationContext syncContext_failed = null;
        private SynchronizationContext syncContext_code = null;
        private SynchronizationContext syncContext_button = null;

        public Form1() {
            InitializeComponent();
            syncContext_tested = SynchronizationContext.Current;
            syncContext_success = SynchronizationContext.Current;
            syncContext_failed = SynchronizationContext.Current;
            syncContext_code = SynchronizationContext.Current;
            syncContext_button = SynchronizationContext.Current;
        }

        private void PostTested(object text) {
            label_tested.Text = text.ToString();
        }

        private void PostSuccess(object text) {
            label_success.Text = text.ToString();
        }

        private void PostFailed(object text) {
            label_failed.Text = text.ToString();
        }

        private void PostCode(object text) {
            label_code.Text = text.ToString();
            if ((int)text >= 400) {
                label_code.ForeColor = Color.Red;
            } else if ((int)text >= 300) {
                label_code.ForeColor = Color.Yellow;
            } else {
                label_code.ForeColor = Color.Black;
            }
        }

        private void PostButton(object text) {
            button1.Text = text.ToString();
        }

        private void button1_Click(object sender, EventArgs e) {
            if (testing) {
                button1.Text = "开始";
                if (thread != null) {
                    thread.Abort();
                }
            } else {
                try {
                    if (textBox1.Text.Substring(0, 7) != "http://" || textBox1.Text.Substring(0, 8) != "https://") {
                        uri = new Uri("http://" + textBox1.Text);
                    }
                } catch {
                    MessageBox.Show("请输入正确的网址", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (textBox3.Text == "0") {
                    MessageBox.Show("超时不能为0", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                timeout = int.Parse(textBox3.Text);
                t = int.Parse(textBox2.Text);
                tested = success = failed = 0;
                thread = new Thread(new ThreadStart(StartRequest));
                thread.Start();
                button1.Text = "停止";
            }
            testing = !testing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (thread != null) {
                thread.Abort();
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) {
            if (radioButton1.Checked) {
                method = "GET";
            } else {
                method = "POST";
            }
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e) {
            if (!(Char.IsNumber(e.KeyChar) || e.KeyChar == '\b')) {
                e.Handled = true;
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e) {
            if (!(Char.IsNumber(e.KeyChar) || e.KeyChar == '\b')) {
                e.Handled = true;
            }
        }

        private void StartRequest() {
            if (t == 0) {
                while (true) {
                    Request();
                }
            } else {
                for(int i = 0; i < t; i++) {
                    Request();
                }
                syncContext_button.Post(PostButton, "开始");
            }
            
        }
        private void Request() {
            int httpStatusCode;
            try {
                tested++;
                WebRequest myReq = WebRequest.Create(uri);
                myReq.Timeout = timeout;
                myReq.Method = method;
                var rsp = myReq.GetResponse() as HttpWebResponse;
                httpStatusCode = (int)rsp.StatusCode;
                rsp.Close();
            } catch {
                httpStatusCode = 404;
            }
            if (httpStatusCode >= 400) {
                failed++;
            } else {
                success++;
            }
            syncContext_code.Post(PostCode, httpStatusCode);
            syncContext_failed.Post(PostFailed, failed);
            syncContext_tested.Post(PostTested, tested);
            syncContext_success.Post(PostSuccess, success);
        }
    }
}
