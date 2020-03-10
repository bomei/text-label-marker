using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using OpenFileDialog=System.Windows.Forms.OpenFileDialog;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.OleDb;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Collections.ObjectModel;

namespace text_label_marker
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public string class_data_filepath = "";
        public string data_filepath = "";
        public HashSet<string>[] tags;
        public int CurrentIndex = 1;
        public List<string> DataToClassify;
        public MainWindow()
        {
            InitializeComponent();
            
            class_data_filepath = Properties.Settings.Default.class_data_file;
            data_filepath = Properties.Settings.Default.data_file;
            class_data_filepath_textbox.Text = class_data_filepath;
            to_classify_data_filepath_textbox.Text = data_filepath;

        }

        private void select_classes_file_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "选择类别文件";
            ofd.Filter = "txt(*.txt*)|*.txt*|所有文件(*.*)|*.*";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                class_data_filepath = ofd.FileName;
                Properties.Settings.Default.class_data_file = ofd.FileName;
                Properties.Settings.Default.Save();
                class_data_filepath_textbox.Text = class_data_filepath;
            }
        }

        private void select_data_file_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "选择待分类文件";
            ofd.Filter = "excel文件(*.xls;*.xlsx)|*.xls;*.xlsx";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                data_filepath = ofd.FileName;
                Properties.Settings.Default.data_file = ofd.FileName;
                Properties.Settings.Default.Save();
                to_classify_data_filepath_textbox.Text = data_filepath;
            }
        }

        public enum FetchDirection
        {
            UP,
            DOWN
        }

        public void UpdateStatement()
        {
            data_statement.Text = DataToClassify[CurrentIndex];
            index_textbox.Content = string.Format("{0}/{1}", CurrentIndex , DataToClassify.Count);
            if (tags[CurrentIndex] == null)
            {
                tags[CurrentIndex] = new HashSet<string>();
            }
            chosen_tags.ItemsSource = tags[CurrentIndex].ToArray();
        }

        public void FetchNextStatement(FetchDirection d)
        {
            switch (d)
            {
                case FetchDirection.UP:
                    if (CurrentIndex > 0)
                    {
                        CurrentIndex--;
                    }
                    break;
                case FetchDirection.DOWN:
                    if (CurrentIndex < DataToClassify.Count - 1)
                    {
                        CurrentIndex++;
                    }
                    break;
                default:
                    break;
            }
            UpdateStatement();
        }

        private void load_button_Click(object sender, RoutedEventArgs e)
        {
            string[] ClassesList = System.IO.File.ReadAllLines(class_data_filepath);
            classes_items.ItemsSource = ClassesList;
            ReadExcel(data_filepath);
            UpdateStatement();
            
        }

        private void Add_Tag_Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            if (tags[CurrentIndex] == null)
            {
                tags[CurrentIndex] = new HashSet<string>();
            }
            tags[CurrentIndex].Add(button.Tag.ToString());
            chosen_tags.ItemsSource = tags[CurrentIndex].ToArray();
            
        }

        private void Delete_Tag_Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            tags[CurrentIndex].Remove(button.Tag.ToString());
            chosen_tags.ItemsSource = tags[CurrentIndex].ToArray();
        }

        public void ReadExcel(string filepath)
        {
            Excel.Application _excelApp = new Excel.Application();
            _excelApp.Visible = false;
            Excel.Workbook workbook = _excelApp.Workbooks.Open(filepath);

            //select the first sheet        
            Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Worksheets[1];
            //find the used range in worksheet
            Excel.Range excelRange = worksheet.UsedRange;

            //get an object array of all of the cells in the worksheet (their values)
            object[,] valueArray = (object[,])excelRange.get_Value(
                        Excel.XlRangeValueDataType.xlRangeValueDefault);

            int taskIndex = 0;
            int markIndex = 0;
            
            for(int col=1; col<excelRange.Columns.Count;col++)
            {
                if(valueArray[1, col].ToString() == "工作任务")
                {
                    taskIndex = col;
                }else if(valueArray[1, col].ToString() == "分类")
                {
                    markIndex = col;
                }
            }
            DataToClassify= new List<string>();
            tags = new HashSet<string>[excelRange.Rows.Count-1];
            for (int row = 2; row < excelRange.Rows.Count; row++)
            {
                DataToClassify.Add(valueArray[row, taskIndex].ToString());
                if (markIndex > 0)
                {
                    string mark = valueArray[row, markIndex].ToString();
                    if (mark != "无" && mark!="") {
                        tags[row - 2] = new HashSet<string>(mark.Split('+'));
                    }
                }
            }
            workbook.Close(0);
            _excelApp.Quit();
            
            return ;
        }

        private void next_button_Click(object sender, RoutedEventArgs e)
        {
            FetchNextStatement(FetchDirection.DOWN);
        }

        private void last_button_Click(object sender, RoutedEventArgs e)
        {
            FetchNextStatement(FetchDirection.UP);
        }

        public void output_button_Click(object sender, RoutedEventArgs e)
        {
            using (StreamWriter outputFile = new StreamWriter(@"output.csv",false,Encoding.UTF8))
            {
                outputFile.WriteLine("分类");
                foreach (var tag in tags)
                {

                    string line = "";
                    if (tag != null)
                    {
                        line = String.Join("+", tag.ToArray());
                    }
                    else
                    {
                        line = "无";
                    }
                    
                    outputFile.WriteLine(line);
                }
                    
            }
            MessageBox.Show("Save output to output.txt");
        }
    }
}
