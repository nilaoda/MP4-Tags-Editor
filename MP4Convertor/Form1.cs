using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;
/// <summary>
/// 2017-2-25 ver 0.1
///           - 初步实现所有功能。
/// </summary>
namespace MP4Convertor
{
    public partial class Form1 : Form
    {
        //拖动窗口
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;

        //不影响点击任务栏图标最大最小化
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_MINIMIZEBOX = 0x00020000;  // Winuser.h中定义
                CreateParams cp = base.CreateParams;
                cp.Style = cp.Style | WS_MINIMIZEBOX;   // 允许最小化操作
                return cp;
            }
        }

        //更新预设
        private void UpdatePresets()
        {
            if (File.Exists("Presets.xml"))  //判断程序目录有无配置文件，并读取文件
            {
                comboBox_Presets.Items.Clear();  //清空条目
                XmlDocument doc = new XmlDocument();
                doc.Load(@"Presets.xml");    //加载Xml文件  
                XmlNode xnl = null;
                xnl = doc.DocumentElement.SelectSingleNode("//Presets");
                //遍历Presets下所有节点，并添加到 comboBox_Presets 中
                if (xnl != null)
                {
                    XmlNodeList xnl1;
                    xnl1 = xnl.ChildNodes;
                    for (int i = 0; i < xnl1.Count; i++)
                    {
                        comboBox_Presets.Items.Add(xnl1[i].Name.ToString());
                    }
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Dispose();
            Application.Exit();
        }

        private void label_Title_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        private void AddFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "视频文件|*.avi;*.mpeg;*.ts;*.mp4;*.m4v;*.3gp;*.3gpp;*.3g2;*.3gp2;*.mkv;*.flv;*.swf;*.m2ts;*.m2t;*.mts;*.f4v";
            fileDialog.FilterIndex = 1;
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] fs = fileDialog.FileNames;
                foreach (string ff in fs)
                    filelist.Items.Add(ff);
            }
        }

        private void Convert_Click(object sender, EventArgs e)
        {
            if (filelist.Items.Count == 0)
            {
                MessageBox.Show("没有数据！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                //开始判断参数
                string meta = "";
                if (textBox_coverPath.Text != "")
                {
                    meta += ":cover=" + "\"" + textBox_coverPath.Text + "\"";
                }
                if (textBox_comment.Text != "")
                {
                    meta += ":comment=" + "\"" + textBox_comment.Text + "\"";
                }
                if (textBox_title.Text != "")
                {
                    meta += ":name=" + "\"" + textBox_title.Text + "\"";
                }
                if (textBox_copyright.Text != "")
                {
                    meta += " -cprt " + "\"" + textBox_copyright.Text + "\"";
                    meta = meta.Replace("  ", " ");
                }

                String TempBat = Path.GetTempPath() + "\\批处理-" + System.DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss") + ".bat";
                StreamWriter bat = new StreamWriter(TempBat, false, Encoding.Default);  //写入数据
                bat.WriteLine("@echo off");
                bat.WriteLine("echo.");
                bat.WriteLine("@echo off");
                bat.WriteLine("@echo off");
                for (int i = 0; i < filelist.Items.Count; i++)
                {
                    OutPath.Text = filelist.Items[i].ToString().Substring(0, filelist.Items[i].ToString().LastIndexOf("\\")) + "\\MP4 Tags Editor";  //获取输出路径
                    OutName.Text = filelist.Items[i].ToString().Substring(filelist.Items[i].ToString().LastIndexOf("\\") + 1, (filelist.Items[i].ToString().LastIndexOf(".") - filelist.Items[i].ToString().LastIndexOf("\\") - 1));  //获取文件名;

                    if (!Directory.Exists(OutPath.Text))//若文件夹不存在则新建文件夹   
                    {
                        Directory.CreateDirectory(OutPath.Text); //新建文件夹   
                    }
                    if (File.Exists(OutPath.Text + "\\" + OutName.Text + ".mp4"))
                    {
                        if (MessageBox.Show("已存在文件，是否覆盖？", "请确认您的操作", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.No)
                        {
                            break;
                        }
                    }

                    if (checkBox_ffmpeg.Checked == true)
                    {
                        bat.WriteLine("TITLE [" + (i+1).ToString("000") + " / " + filelist.Items.Count.ToString("000") + "] - MP4 Tags Editor 2.0 @ nilaoda");
                        bat.WriteLine(("\"" + Environment.CurrentDirectory + "\\ffmpeg.exe" + "\" -i \"" + filelist.Items[i].ToString()
                                + "\" -y -map 0:v -vcodec copy -map 0:a -acodec copy -bsf:a aac_adtstoasc " + "\""
                                + OutPath.Text + "\\" + "tempformp4box" + ".mp4\"").Replace("%", "%%"));
                        bat.WriteLine(("\"" + Environment.CurrentDirectory + "\\MP4Box.exe" + "\" -tmp "
                            + "\"" + OutPath.Text + "\\" + "MP4 Tags Editor" + "\""
                            + " -inter 0 -add "
                            + "\"" + OutPath.Text + "\\tempformp4box.mp4" + "\""
                            + " -itags tool=\"MP4 Tags Editor @ nilaoda\"" + meta
                            + " -new " + "\"" + OutPath.Text + "\\" + OutName.Text + ".mp4\"").Replace("%", "%%"));
                        if (checkBox_delSource.Checked == true)
                        {
                            bat.WriteLine("del " + "\"" + filelist.Items[i].ToString() + "\"");
                        }
                    }
                    else
                    {
                        bat.WriteLine("TITLE [" + (i + 1).ToString("000") + " / " + filelist.Items.Count.ToString("000") + "] - MP4 Tags Editor 2.0 @ nilaoda");
                        bat.WriteLine(("\"" + Environment.CurrentDirectory + "\\MP4Box.exe" + "\" -tmp " 
                            + "\"" + OutPath.Text + "\\" + "MP4 Tags Editor" + "\"" 
                            + " -inter 0 -add "
                            + "\"" + filelist.Items[i].ToString() + "\""
                            + " -itags tool=\"MP4 Tags Editor @ nilaoda\"" + meta
                            + " -new " + "\"" + OutPath.Text + "\\" + OutName.Text + ".mp4\"").Replace("%", "%%"));
                        if (checkBox_delSource.Checked == true)
                        {
                            bat.WriteLine("del " + "\"" + filelist.Items[i].ToString() + "\"");
                        }
                    }

                    if (i == filelist.Items.Count - 1 && checkBox_ffmpeg.Checked == true) 
                    {
                        bat.WriteLine("del " + "\"" + OutPath.Text + "\\tempformp4box.mp4" + "\"");
                    }
                }
                bat.Close();
                Process.Start(TempBat);
            }
        }

        private void filelist_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                //获取拖拽的文件地址
                var filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                var hz = filenames[0].LastIndexOf('.') + 1;
                var houzhui = filenames[0].Substring(hz);//文件后缀名
                if (houzhui == "m3u8" || houzhui == "mkv" || houzhui == "avi" || houzhui == "mp4" || houzhui == "ts" || houzhui == "flv" || houzhui == "f4v" ||
                    houzhui == "TS" || houzhui == "wm" || houzhui == "mpeg" || houzhui == "mpg" || houzhui == "m4v" || houzhui == "3gp" || houzhui == "rm" ||
                    houzhui == "MP4" || houzhui == "m2ts") //只允许拖入部分文件
                {
                    String[] files = (String[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (String s in files)
                    {
                        (sender as ListBox).Items.Add(s);
                    }
                }

            }
        }

        private void filelist_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void filelist_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(@"ffmpeg.exe"))  //判断程序目录有无ffmpeg.exe
            {
            }
            else
            {
                MessageBox.Show("没有找到ffmpeg.exe", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Dispose();
                Application.Exit();
            }
            if (File.Exists(@"MP4Box.exe"))  //判断程序目录有无ffmpeg.exe
            {
            }
            else
            {
                MessageBox.Show("没有找到MP4Box.exe", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Dispose();
                Application.Exit();
            }
            UpdatePresets();  //更新预设
        }

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filelist.SelectedItem != null)
            {
                filelist.Items.Remove(filelist.SelectedItem);
            }
        }

        private void 删除全部ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            filelist.Items.Clear();
        }

        //右键选中并弹出菜单
        private void filelist_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

                int index = filelist.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    filelist.SelectedIndex = index;
                    this.contextMenuStrip1.Show(filelist, e.Location);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void label1_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        private void checkBox_ffmpeg_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_ffmpeg.CheckState == CheckState.Checked)
            {
                checkBox_ffmpeg.ForeColor = Color.FromArgb(46, 204, 113);
            }
            if (checkBox_ffmpeg.CheckState == CheckState.Unchecked)
            {
                checkBox_ffmpeg.ForeColor = Color.FromArgb(52, 152, 219);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "图片文件|*.jpg;*.png";
            fileDialog.FilterIndex = 1;
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox_coverPath.Text = fileDialog.FileName;
            }
        }

        private void checkBox_delSource_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_delSource.CheckState == CheckState.Checked)
            {
                checkBox_delSource.ForeColor = Color.FromArgb(46, 204, 113);
            }
            if (checkBox_delSource.CheckState == CheckState.Unchecked)
            {
                checkBox_delSource.ForeColor = Color.FromArgb(52, 152, 219);
            }
        }

        private void button_Presets_Add_Click(object sender, EventArgs e)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(@"Presets.xml");    //加载Xml文件  

                var root = doc.DocumentElement;//取到根结点
                XmlNode node = doc.CreateElement("_" + textBox_Preset_Name.Text);

                XmlElement name = doc.CreateElement("name");
                XmlCDataSection nameCdata = doc.CreateCDataSection(textBox_title.Text);
                name.AppendChild(nameCdata);
                node.AppendChild(name);
                XmlElement cover = doc.CreateElement("cover");
                XmlCDataSection coverCdata = doc.CreateCDataSection(textBox_coverPath.Text);
                cover.AppendChild(coverCdata);
                node.AppendChild(cover);
                XmlElement copyright = doc.CreateElement("copyright");
                XmlCDataSection copyrightCdata = doc.CreateCDataSection(textBox_copyright.Text);
                copyright.AppendChild(copyrightCdata);
                node.AppendChild(copyright);
                XmlElement comment = doc.CreateElement("comment");
                XmlCDataSection commentCdata = doc.CreateCDataSection(textBox_comment.Text);
                comment.AppendChild(commentCdata);
                node.AppendChild(comment);

                root.AppendChild(node);

                doc.Save(@"Presets.xml");    //保存Xml文件  
                MessageBox.Show("已保存预设：\n\"" + textBox_Preset_Name.Text + "\"", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                MessageBox.Show("· 名称可以含字母、数字以及其他的字符\r\n"
                    + "· 名称不能以数字或者标点符号开始\r\n"
                    + "· 名称不能以字符 xml（XML、Xml、xML等）开始\r\n"
                    + "· 名称不能包含空格\r\n"
                    + "· 尽量避免使用\"-\"和\".\"，因为可能引起混乱\r\n"
                    + "· 在XML元素命名中不要使用\":\"\r\n"
                    + "请多使用下划线\"_\""
                    , "XML 元素必须遵循以下命名规则：", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdatePresets();  //更新预设
        }

        private void button_Presets_Cut_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定删除\"" + comboBox_Presets.Text + "\"吗？", "请确认您的操作", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (comboBox_Presets.Text == "" || comboBox_Presets.Text == "_Default")
                {
                    MessageBox.Show("你无法删除此条目！", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(@"Presets.xml");

                    XmlElement root = doc.DocumentElement;  //根节点

                    foreach (XmlElement xn in root.ChildNodes)
                    {
                        if (xn.Name.Equals(comboBox_Presets.Text))
                        {
                            root.RemoveChild(xn);
                            doc.Save(@"Presets.xml");
                        }
                    }

                    UpdatePresets();  //更新预设
                }
            }
            else
            {

            }
        }

        private void comboBox_Presets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (File.Exists("Presets.xml"))  //判断程序目录有无配置文件，并读取文件
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(@"Presets.xml");    //加载Xml文件
                XmlNodeList topM = doc.SelectNodes(@"/Presets/" + comboBox_Presets.Text);
                foreach (XmlElement element in topM)
                {
                    textBox_title.Text = element.GetElementsByTagName("name")[0].InnerText;
                    textBox_coverPath.Text = element.GetElementsByTagName("cover")[0].InnerText;
                    textBox_copyright.Text = element.GetElementsByTagName("copyright")[0].InnerText;
                    textBox_comment.Text = element.GetElementsByTagName("comment")[0].InnerText;
                }
            }
        }
    }
}
