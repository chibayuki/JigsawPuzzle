/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
Copyright © 2013-2018 chibayuki@foxmail.com

拼图板
Version 7.1.17000.4925.R16.180602-0000

This file is part of 拼图板

拼图板 is released under the GPLv3 license
* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace WinFormApp
{
    public partial class Form_Main : Form
    {
        #region 版本信息

        private static readonly string ApplicationName = Application.ProductName; // 程序名。
        private static readonly string ApplicationEdition = "7.1.16"; // 程序版本。

        private static readonly Int32 MajorVersion = new Version(Application.ProductVersion).Major; // 主版本。
        private static readonly Int32 MinorVersion = new Version(Application.ProductVersion).Minor; // 副版本。
        private static readonly Int32 BuildNumber = new Version(Application.ProductVersion).Build; // 版本号。
        private static readonly Int32 BuildRevision = new Version(Application.ProductVersion).Revision; // 修订版本。
        private static readonly string LabString = "R16"; // 分支名。
        private static readonly string BuildTime = "180602-0000"; // 编译时间。

        //

        private static readonly string RootDir_Product = Environment.SystemDirectory.Substring(0, 1) + @":\ProgramData\AppConfig\jigsaw"; // 根目录：此产品。
        private static readonly string RootDir_CurrentVersion = RootDir_Product + "\\" + BuildNumber + "." + BuildRevision; // 根目录：当前版本。

        private static readonly string ConfigFileDir = RootDir_CurrentVersion + @"\Config"; // 配置文件所在目录。
        private static readonly string ConfigFilePath = ConfigFileDir + @"\settings.cfg"; // 配置文件路径。

        private static readonly string ResFileDir = RootDir_CurrentVersion + @"\Res"; // 资源文件所在目录。
        private static readonly string ImgresFilePath = ResFileDir + @"\bkg.img"; // 图像资源文件路径。

        private static readonly string LogFileDir = RootDir_CurrentVersion + @"\Log"; // 存档文件所在目录。
        private static readonly string DataFilePath = LogFileDir + @"\userdata.cfg"; // 用户数据文件路径（包含最佳成绩与游戏时长）。
        private static readonly string RecordFilePath = LogFileDir + @"\lastgame.cfg"; // 上次游戏文件路径（包含最后一次游戏记录）。

        //

        private static readonly List<Version> OldVersionList = new List<Version> // 兼容的版本列表，用于从最新的兼容版本迁移配置设置。
        {
            new Version(7, 1, 17000, 0),
            new Version(7, 1, 17000, 255),
            new Version(7, 1, 17000, 505),
            new Version(7, 1, 17000, 534),
            new Version(7, 1, 17000, 726),
            new Version(7, 1, 17000, 928),
            new Version(7, 1, 17000, 1154),
            new Version(7, 1, 17000, 3561),
            new Version(7, 1, 17000, 4247),
            new Version(7, 1, 17000, 4464),
            new Version(7, 1, 17000, 4651),
            new Version(7, 1, 17000, 4729),
            new Version(7, 1, 17000, 4807),/*
            new Version(7, 1, 17000, 4925)*/
        };

        //

        private static readonly string URL_GitHub_Base = @"https://github.com/chibayuki/JigsawPuzzle"; // 此项目在 GitHub 的 URL。
        private static readonly string URL_GitHub_Release = URL_GitHub_Base + @"/releases"; // 此项目的发布版本在 GitHub 的 URL。

        #endregion

        #region 配置设置变量

        private static readonly Size Range_MAX = new Size(CAPACITY, CAPACITY); // 最大界面布局。
        private static readonly Size Range_MIN = new Size(3, 3); // 最小界面布局。
        private Size Range = new Size(4, 4); // 当前界面布局（以元素数为单位）。

        //

        private enum BlockStyles { NULL = -1, Number, Image, COUNT } // 区块样式枚举。
        private BlockStyles BlockStyle = BlockStyles.Number; // 当前区块样式。

        private bool ShowNumberOnImage = false; // 当区块样式为 BlockStyles.Image 时，是否在图片上显示数字。

        //

        private const Com.WinForm.Theme Theme_DEFAULT = Com.WinForm.Theme.Colorful; // 主题的默认值。

        private bool UseRandomThemeColor = true; // 是否使用随机的主题颜色。

        private static readonly Color ThemeColor_DEFAULT = Color.Gray; // 主题颜色的默认值。

        private const bool ShowFormTitleColor_DEFAULT = true; // 是否显示窗体标题栏的颜色的默认值。

        private const double Opacity_MIN = 0.05; // 总体不透明度的最小值。
        private const double Opacity_MAX = 1.0; // 总体不透明度的最大值。

        //

        private bool AntiAlias = true; // 是否使用抗锯齿模式绘图。

        #endregion

        #region 元素矩阵变量

        private const Int32 CAPACITY = 15; // 元素矩阵容量的平方根。

        private Int32[,] ElementArray = new Int32[CAPACITY, CAPACITY]; // 元素矩阵。

        private List<Point> ElementIndexList = new List<Point>(CAPACITY * CAPACITY); // 元素索引列表。

        //

        private Int32 ElementSize = 160; // 元素边长。

        #endregion

        #region 游戏变量

        private static readonly Size FormClientInitialSize = new Size(585, 420); // 窗体工作区初始大小。

        //

        private Color GameUIBackColor_DEC => Me.RecommendColors.Background_DEC.ToColor(); // 游戏 UI 背景颜色（浅色）。
        private Color GameUIBackColor_INC => Me.RecommendColors.Background_INC.ToColor(); // 游戏 UI 背景颜色（深色）。

        //

        private bool GameIsWin = false; // 游戏是否已经完成。

        //

        private TimeSpan ThisGameTime = TimeSpan.Zero; // 本次游戏时长。
        private TimeSpan TotalGameTime = TimeSpan.Zero; // 累计游戏时长。

        //

        private struct Record // 记录。
        {
            public Size Range; // 布局。

            public TimeSpan GameTime; // 游戏用时。
            public Int32 StepCount; // 操作步数。
        }

        private Record[,] BestRecordArray = new Record[Range_MAX.Width - Range_MIN.Width + 1, Range_MAX.Height - Range_MIN.Height + 1]; // 最高分记录矩阵。
        private Record BestRecord // 获取或设置当前界面布局下的最高分记录。
        {
            get
            {
                return BestRecordArray[Range.Width - Range_MIN.Width, Range.Height - Range_MIN.Height];
            }

            set
            {
                BestRecordArray[Range.Width - Range_MIN.Width, Range.Height - Range_MIN.Height] = value;
            }
        }

        private Record ThisRecord = new Record(); // 本次记录。

        //

        private Record Record_Last = new Record(); // 上次游戏的记录。

        private Int32[,] ElementArray_Last = new Int32[CAPACITY, CAPACITY]; // 上次游戏的元素矩阵。

        private List<Point> ElementIndexList_Last = new List<Point>(0); // 上次游戏的元素索引列表。

        #endregion

        #region 计时器数据

        private struct CycData // 计时周期数据。
        {
            private Int32 _Tick0;
            private Int32 _Tick1;

            public double DeltaMS // 当前周期的毫秒数。
            {
                get
                {
                    return Math.Abs(_Tick0 - _Tick1);
                }
            }

            private Int32 _Cnt; // 周期计数。
            public Int32 Cnt
            {
                get
                {
                    return _Cnt;
                }
            }

            private double _Avg_Am; // 周期毫秒数的算数平均值。
            public double Avg_Am
            {
                get
                {
                    return _Avg_Am;
                }
            }

            private double _Avg_St; // 周期毫秒数的统计平均值。
            public double Avg_St
            {
                get
                {
                    return _Avg_St;
                }
            }

            public void Reset() // 重置此结构。
            {
                _Tick0 = _Tick1 = Environment.TickCount;
                _Cnt = 0;
                _Avg_Am = _Avg_St = 0;
            }

            public void Update() // 更新此结构。
            {
                if (_Tick0 <= _Tick1)
                {
                    _Tick0 = Environment.TickCount;
                }
                else
                {
                    _Tick1 = Environment.TickCount;
                }

                _Cnt = Math.Min(1048576, _Cnt + 1);

                _Avg_Am = (_Avg_Am + DeltaMS) / 2;
                _Avg_St = _Avg_St * (_Cnt - 1) / _Cnt + DeltaMS / _Cnt;
            }
        }

        #endregion

        #region 窗体构造

        private Com.WinForm.FormManager Me;

        public Com.WinForm.FormManager FormManager
        {
            get
            {
                return Me;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_MINIMIZEBOX = 0x00020000;

                CreateParams CP = base.CreateParams;

                if (Me != null && Me.FormStyle != Com.WinForm.FormStyle.Dialog)
                {
                    CP.Style = CP.Style | WS_MINIMIZEBOX;
                }

                return CP;
            }
        }

        private void _Ctor(Com.WinForm.FormManager owner)
        {
            InitializeComponent();

            //

            if (owner != null)
            {
                Me = new Com.WinForm.FormManager(this, owner);
            }
            else
            {
                Me = new Com.WinForm.FormManager(this);
            }

            //

            FormDefine();
        }

        public Form_Main()
        {
            _Ctor(null);
        }

        public Form_Main(Com.WinForm.FormManager owner)
        {
            _Ctor(owner);
        }

        private void FormDefine()
        {
            Me.Caption = ApplicationName;
            Me.FormStyle = Com.WinForm.FormStyle.Sizable;
            Me.EnableFullScreen = true;
            Me.ClientSize = FormClientInitialSize;
            Me.Theme = Theme_DEFAULT;
            Me.ThemeColor = new Com.ColorX(ThemeColor_DEFAULT);
            Me.ShowCaptionBarColor = ShowFormTitleColor_DEFAULT;

            Me.Loading += LoadingEvents;
            Me.Loaded += LoadedEvents;
            Me.Closed += ClosedEvents;
            Me.Resize += ResizeEvents;
            Me.SizeChanged += SizeChangedEvents;
            Me.FormStateChanged += FormStateChangedEvents;
            Me.ThemeChanged += ThemeColorChangedEvents;
            Me.ThemeColorChanged += ThemeColorChangedEvents;
        }

        #endregion

        #region 窗体事件

        private void LoadingEvents(object sender, EventArgs e)
        {
            //
            // 在窗体加载时发生。
            //

            TransConfig();

            DelOldConfig();

            LoadConfig();

            LoadUserData();

            LoadLastGame();

            //

            if (UseRandomThemeColor)
            {
                Me.ThemeColor = Com.ColorManipulation.GetRandomColorX();
            }
        }

        private void LoadedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体加载后发生。
            //

            Me.OnSizeChanged();
            Me.OnThemeChanged();

            //

            ComboBox_Range_Width.SelectedIndexChanged -= ComboBox_Range_Width_SelectedIndexChanged;
            ComboBox_Range_Height.SelectedIndexChanged -= ComboBox_Range_Height_SelectedIndexChanged;

            for (int i = Range_MIN.Width; i <= Range_MAX.Width; i++)
            {
                ComboBox_Range_Width.Items.Add(i.ToString());
            }

            for (int i = Range_MIN.Height; i <= Range_MAX.Height; i++)
            {
                ComboBox_Range_Height.Items.Add(i.ToString());
            }

            ComboBox_Range_Width.SelectedIndex = ComboBox_Range_Width.Items.IndexOf(Range.Width.ToString());
            ComboBox_Range_Height.SelectedIndex = ComboBox_Range_Height.Items.IndexOf(Range.Height.ToString());

            ComboBox_Range_Width.SelectedIndexChanged += ComboBox_Range_Width_SelectedIndexChanged;
            ComboBox_Range_Height.SelectedIndexChanged += ComboBox_Range_Height_SelectedIndexChanged;

            //

            RadioButton_UseRandomThemeColor.CheckedChanged -= RadioButton_UseRandomThemeColor_CheckedChanged;
            RadioButton_UseCustomColor.CheckedChanged -= RadioButton_UseCustomColor_CheckedChanged;

            if (UseRandomThemeColor)
            {
                RadioButton_UseRandomThemeColor.Checked = true;
            }
            else
            {
                RadioButton_UseCustomColor.Checked = true;
            }

            RadioButton_UseRandomThemeColor.CheckedChanged += RadioButton_UseRandomThemeColor_CheckedChanged;
            RadioButton_UseCustomColor.CheckedChanged += RadioButton_UseCustomColor_CheckedChanged;

            Label_ThemeColorName.Enabled = !UseRandomThemeColor;

            //

            CheckBox_AntiAlias.CheckedChanged -= CheckBox_AntiAlias_CheckedChanged;

            CheckBox_AntiAlias.Checked = AntiAlias;

            CheckBox_AntiAlias.CheckedChanged += CheckBox_AntiAlias_CheckedChanged;

            //

            ResetBlockStyleControl();

            //

            Label_ApplicationName.Text = ApplicationName;
            Label_ApplicationEdition.Text = ApplicationEdition;
            Label_Version.Text = "版本: " + MajorVersion + "." + MinorVersion + "." + BuildNumber + "." + BuildRevision;

            //

            Com.WinForm.ControlSubstitution.LabelAsButton(Label_StartNewGame, Label_StartNewGame_Click);
            Com.WinForm.ControlSubstitution.LabelAsButton(Label_ContinueLastGame, Label_ContinueLastGame_Click);

            //

            FunctionAreaTab = FunctionAreaTabs.Start;
        }

        private void ClosedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体关闭后发生。
            //

            SaveConfig();

            if (GameUINow)
            {
                Interrupt(InterruptActions.CloseApp);
            }
        }

        private void ResizeEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的大小调整时发生。
            //

            Panel_FunctionArea.Size = Panel_GameUI.Size = Panel_Client.Size = Panel_Main.Size;

            Panel_FunctionAreaOptionsBar.Size = new Size(Panel_FunctionArea.Width / 3, Panel_FunctionArea.Height);
            Label_Tab_Start.Size = Label_Tab_Record.Size = Label_Tab_Options.Size = Label_Tab_About.Size = new Size(Panel_FunctionAreaOptionsBar.Width, Panel_FunctionAreaOptionsBar.Height / 4);
            Label_Tab_Record.Top = Label_Tab_Start.Bottom;
            Label_Tab_Options.Top = Label_Tab_Record.Bottom;
            Label_Tab_About.Top = Label_Tab_Options.Bottom;

            Panel_FunctionAreaTab.Left = Panel_FunctionAreaOptionsBar.Right;
            Panel_FunctionAreaTab.Size = new Size(Panel_FunctionArea.Width - Panel_FunctionAreaOptionsBar.Width, Panel_FunctionArea.Height);

            Func<Control, Control, Size> GetTabSize = (Tab, Container) => new Size(Container.Width - (Container.Height < Tab.MinimumSize.Height ? 25 : 0), Container.Height - (Container.Width < Tab.MinimumSize.Width ? 25 : 0));

            Panel_Tab_Start.Size = GetTabSize(Panel_Tab_Start, Panel_FunctionAreaTab);
            Panel_Tab_Record.Size = GetTabSize(Panel_Tab_Record, Panel_FunctionAreaTab);
            Panel_Tab_Options.Size = GetTabSize(Panel_Tab_Options, Panel_FunctionAreaTab);
            Panel_Tab_About.Size = GetTabSize(Panel_Tab_About, Panel_FunctionAreaTab);

            //

            Panel_EnterGameSelection.Location = new Point((Panel_Tab_Start.Width - Panel_EnterGameSelection.Width) / 2, (Panel_Tab_Start.Height - Panel_EnterGameSelection.Height) / 2);

            Panel_Score.Width = Panel_Tab_Record.Width - Panel_Score.Left * 2;
            Panel_Score.Height = Panel_Tab_Record.Height - Panel_Score.Top * 2 - Panel_GameTime.Height;
            Panel_GameTime.Width = Panel_Tab_Record.Width - Panel_GameTime.Left * 2;
            Panel_GameTime.Top = Panel_Score.Bottom;
            Label_ThisRecord.Location = new Point(Math.Max(0, Math.Min(Panel_Score.Width - Label_ThisRecord.Width, (Panel_Score.Width / 2 - Label_ThisRecord.Width) / 2)), Panel_Score.Height - 25 - Label_ThisRecord.Height);
            Label_BestRecord.Location = new Point(Math.Max(0, Math.Min(Panel_Score.Width - Label_BestRecord.Width, Panel_Score.Width / 2 + (Panel_Score.Width / 2 - Label_BestRecord.Width) / 2)), Panel_Score.Height - 25 - Label_BestRecord.Height);

            Panel_Range.Width = Panel_Tab_Options.Width - Panel_Range.Left * 2;
            Panel_BlockStyle.Width = Panel_Tab_Options.Width - Panel_BlockStyle.Left * 2;
            Panel_ThemeColor.Width = Panel_Tab_Options.Width - Panel_ThemeColor.Left * 2;
            Panel_AntiAlias.Width = Panel_Tab_Options.Width - Panel_AntiAlias.Left * 2;

            //

            Panel_Current.Width = Panel_GameUI.Width;

            Panel_Interrupt.Left = Panel_Current.Width - Panel_Interrupt.Width;

            Panel_Environment.Size = new Size(Panel_GameUI.Width, Panel_GameUI.Height - Panel_Environment.Top);
        }

        private void SizeChangedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的大小更改时发生。
            //

            if (Panel_GameUI.Visible)
            {
                ElementSize = Math.Max(1, Math.Min(Panel_Environment.Width / Range.Width, Panel_Environment.Height / Range.Height));

                EAryBmpRect.Size = new Size(Math.Max(1, ElementSize * Range.Width), Math.Max(1, ElementSize * Range.Height));
                EAryBmpRect.Location = new Point((Panel_Environment.Width - EAryBmpRect.Width) / 2, (Panel_Environment.Height - EAryBmpRect.Height) / 2);

                RepaintCurBmp();

                ElementArray_RepresentAll();
            }

            if (Panel_FunctionArea.Visible && FunctionAreaTab == FunctionAreaTabs.Record)
            {
                Panel_Tab_Record.Refresh();
            }
        }

        private void FormStateChangedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的状态更改时发生。
            //

            if (Me.FormState == Com.WinForm.FormState.Minimized && Timer_Timer.Enabled)
            {
                Interrupt(InterruptActions.Pause);
            }
        }

        private void ThemeColorChangedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的主题色更改时发生。
            //

            // 功能区选项卡

            Panel_FunctionArea.BackColor = Me.RecommendColors.Background_DEC.ToColor();
            Panel_FunctionAreaOptionsBar.BackColor = Me.RecommendColors.Main.ToColor();

            FunctionAreaTab = _FunctionAreaTab;

            // "记录"区域

            Label_ThisRecord.ForeColor = Label_BestRecord.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_ThisRecordVal_GameTime.ForeColor = Label_BestRecordVal_GameTime.ForeColor = Me.RecommendColors.Text_INC.ToColor();
            Label_ThisRecordVal_StepCount.ForeColor = Label_BestRecordVal_StepCount.ForeColor = Me.RecommendColors.Text.ToColor();

            Label_ThisTime.ForeColor = Label_TotalTime.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_ThisTimeVal.ForeColor = Label_TotalTimeVal.ForeColor = Me.RecommendColors.Text_INC.ToColor();

            // "选项"区域

            Label_Range.ForeColor = Label_BlockStyle.ForeColor = Label_ThemeColor.ForeColor = Label_AntiAlias.ForeColor = Me.RecommendColors.Text_INC.ToColor();

            Label_Range_Width.ForeColor = Label_Range_Height.ForeColor = Me.RecommendColors.Text.ToColor();

            ComboBox_Range_Width.BackColor = ComboBox_Range_Height.BackColor = Me.RecommendColors.MenuItemBackground.ToColor();
            ComboBox_Range_Width.ForeColor = ComboBox_Range_Height.ForeColor = Me.RecommendColors.MenuItemText.ToColor();

            RadioButton_Number.ForeColor = RadioButton_Image.ForeColor = Me.RecommendColors.Text.ToColor();

            CheckBox_ShowNumberOnImage.ForeColor = Me.RecommendColors.Text.ToColor();

            Panel_BkgImg.BackColor = Me.RecommendColors.Background_INC.ToColor();

            Label_SelectBkgImg_WithText.BackColor = Color.FromArgb(192, Panel_BkgImg.BackColor);
            Label_SelectBkgImg_WithText.ForeColor = Me.RecommendColors.Text.ToColor();

            RadioButton_UseRandomThemeColor.ForeColor = RadioButton_UseCustomColor.ForeColor = Me.RecommendColors.Text.ToColor();

            Label_ThemeColorName.Text = Com.ColorManipulation.GetColorName(Me.ThemeColor.ToColor());
            Label_ThemeColorName.ForeColor = Me.RecommendColors.Text.ToColor();

            CheckBox_AntiAlias.ForeColor = Me.RecommendColors.Text.ToColor();

            // "关于"区域

            Label_ApplicationName.ForeColor = Me.RecommendColors.Text_INC.ToColor();
            Label_ApplicationEdition.ForeColor = Label_Version.ForeColor = Label_Copyright.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_GitHub_Part1.ForeColor = Label_GitHub_Base.ForeColor = Label_GitHub_Part2.ForeColor = Label_GitHub_Release.ForeColor = Me.RecommendColors.Text.ToColor();

            // 控件替代

            Com.WinForm.ControlSubstitution.PictureBoxAsButton(PictureBox_Interrupt, PictureBox_Interrupt_Click, null, PictureBox_Interrupt_MouseEnter, null, Color.Transparent, Me.RecommendColors.Button_INC.AtOpacity(50).ToColor(), Me.RecommendColors.Button_INC.AtOpacity(70).ToColor());
            Com.WinForm.ControlSubstitution.PictureBoxAsButton(PictureBox_Restart, PictureBox_Restart_Click, null, PictureBox_Restart_MouseEnter, null, Color.Transparent, Me.RecommendColors.Button_INC.AtOpacity(50).ToColor(), Me.RecommendColors.Button_INC.AtOpacity(70).ToColor());
            Com.WinForm.ControlSubstitution.PictureBoxAsButton(PictureBox_ExitGame, PictureBox_ExitGame_Click, null, PictureBox_ExitGame_MouseEnter, null, Color.Transparent, Me.RecommendColors.Button_INC.AtOpacity(50).ToColor(), Me.RecommendColors.Button_INC.AtOpacity(70).ToColor());

            Com.WinForm.ControlSubstitution.LabelAsButton(Label_ThemeColorName, Label_ThemeColorName_Click, Color.Transparent, Me.RecommendColors.Button_DEC.ToColor(), Me.RecommendColors.Button_INC.ToColor(), new Font("微软雅黑", 9.75F, FontStyle.Underline, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134));

            Com.WinForm.ControlSubstitution.LabelAsButton(Label_GitHub_Base, Label_GitHub_Base_Click, Color.Transparent, Me.RecommendColors.Button_DEC.ToColor(), Me.RecommendColors.Button_INC.ToColor(), new Font("微软雅黑", 9.75F, FontStyle.Underline, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134));
            Com.WinForm.ControlSubstitution.LabelAsButton(Label_GitHub_Release, Label_GitHub_Release_Click, Color.Transparent, Me.RecommendColors.Button_DEC.ToColor(), Me.RecommendColors.Button_INC.ToColor(), new Font("微软雅黑", 9.75F, FontStyle.Underline, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134));
        }

        #endregion

        #region 背景绘图

        private void Panel_FunctionAreaOptionsBar_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_FunctionAreaOptionsBar 绘图。
            //

            Graphics Grap = e.Graphics;
            Grap.SmoothingMode = SmoothingMode.AntiAlias;

            //

            Control[] TabCtrl = new Control[(Int32)FunctionAreaTabs.COUNT] { Label_Tab_Start, Label_Tab_Record, Label_Tab_Options, Label_Tab_About };

            List<bool> TabBtnPointed = new List<bool>(TabCtrl.Length);
            List<bool> TabBtnSeld = new List<bool>(TabCtrl.Length);

            for (int i = 0; i < TabCtrl.Length; i++)
            {
                TabBtnPointed.Add(Com.Geometry.CursorIsInControl(TabCtrl[i]));
                TabBtnSeld.Add(FunctionAreaTab == (FunctionAreaTabs)i);
            }

            Color TabBtnCr_Bk_Pointed = Color.FromArgb(128, Color.White), TabBtnCr_Bk_Seld = Color.FromArgb(192, Color.White), TabBtnCr_Bk_Uns = Color.FromArgb(64, Color.White);

            for (int i = 0; i < TabCtrl.Length; i++)
            {
                Color TabBtnCr_Bk = (TabBtnSeld[i] ? TabBtnCr_Bk_Seld : (TabBtnPointed[i] ? TabBtnCr_Bk_Pointed : TabBtnCr_Bk_Uns));

                GraphicsPath Path_TabBtn = new GraphicsPath();
                Path_TabBtn.AddRectangle(TabCtrl[i].Bounds);
                PathGradientBrush PGB_TabBtn = new PathGradientBrush(Path_TabBtn)
                {
                    CenterColor = Color.FromArgb(TabBtnCr_Bk.A / 2, TabBtnCr_Bk),
                    SurroundColors = new Color[] { TabBtnCr_Bk },
                    FocusScales = new PointF(1F, 0F)
                };
                Grap.FillPath(PGB_TabBtn, Path_TabBtn);
                Path_TabBtn.Dispose();
                PGB_TabBtn.Dispose();

                if (TabBtnSeld[i])
                {
                    PointF[] Polygon = new PointF[] { new PointF(TabCtrl[i].Right, TabCtrl[i].Top + TabCtrl[i].Height / 4), new PointF(TabCtrl[i].Right - TabCtrl[i].Height / 4, TabCtrl[i].Top + TabCtrl[i].Height / 2), new PointF(TabCtrl[i].Right, TabCtrl[i].Bottom - TabCtrl[i].Height / 4) };

                    Grap.FillPolygon(new SolidBrush(Panel_FunctionArea.BackColor), Polygon);
                }
            }
        }

        private void Panel_Score_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_Score 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = PictureBox_Score;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }

            //

            PaintScore(e);
        }

        private void Panel_GameTime_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_GameTime 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = PictureBox_GameTime;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        private void Panel_Range_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_Range 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = Label_Range;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        private void Panel_BlockStyle_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_BlockStyle 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = Label_BlockStyle;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        private void Panel_ThemeColor_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_ThemeColor 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = Label_ThemeColor;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        private void Panel_AntiAlias_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_AntiAlias 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = Label_AntiAlias;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        #endregion

        #region 配置设置

        private void TransConfig()
        {
            //
            // 从当前内部版本号下最近的旧版本迁移配置文件。
            //

            try
            {
                if (!Directory.Exists(RootDir_CurrentVersion))
                {
                    if (OldVersionList.Count > 0)
                    {
                        List<Version> OldVersionList_Copy = new List<Version>(OldVersionList);
                        List<Version> OldVersionList_Sorted = new List<Version>(0);

                        while (OldVersionList_Copy.Count > 0)
                        {
                            Version NewestVersion = OldVersionList_Copy[0];

                            foreach (var V in OldVersionList_Copy)
                            {
                                if (NewestVersion <= V)
                                {
                                    NewestVersion = V;
                                }
                            }

                            OldVersionList_Sorted.Add(NewestVersion);
                            OldVersionList_Copy.Remove(NewestVersion);
                        }

                        for (int i = 0; i < OldVersionList_Sorted.Count; i++)
                        {
                            if (Directory.Exists(RootDir_Product + "\\" + OldVersionList_Sorted[i].Build + "." + OldVersionList_Sorted[i].Revision))
                            {
                                try
                                {
                                    Com.IO.CopyFolder(RootDir_Product + "\\" + OldVersionList_Sorted[i].Build + "." + OldVersionList_Sorted[i].Revision, RootDir_CurrentVersion);

                                    break;
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void DelOldConfig()
        {
            //
            // 删除当前内部版本号下所有旧版本的配置文件。
            //

            try
            {
                if (OldVersionList.Count > 0)
                {
                    foreach (var V in OldVersionList)
                    {
                        Com.IO.DeleteFolder(RootDir_Product + "\\" + V.Build + "." + V.Revision);
                    }
                }
            }
            catch { }
        }

        private void LoadConfig()
        {
            //
            // 加载配置文件。
            //

            if (File.Exists(ConfigFilePath))
            {
                if (new FileInfo(ConfigFilePath).Length > 0)
                {
                    StreamReader Read = new StreamReader(ConfigFilePath, false);
                    string Cfg = Read.ReadLine();
                    Read.Close();

                    Regex RegexUint = new Regex(@"[^0-9]");
                    Regex RegexFloat = new Regex(@"[^0-9\-\.]");

                    //

                    try
                    {
                        string SubStr = RegexUint.Replace(Com.Text.GetIntervalString(Cfg, "<ElementSize>", "</ElementSize>", false, false), string.Empty);

                        ElementSize = Convert.ToInt32(SubStr);
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Cfg, "<Range>", "</Range>", false, false);

                        string[] Fields = SubStr.Split(',');

                        if (Fields.Length == 2)
                        {
                            int i = 0;

                            string StrW = RegexUint.Replace(Fields[i++], string.Empty);
                            string StrH = RegexUint.Replace(Fields[i++], string.Empty);

                            Size R = new Size(Convert.ToInt32(StrW), Convert.ToInt32(StrH));

                            if (R.Width >= Range_MIN.Width && R.Width <= Range_MAX.Width && R.Height >= Range_MIN.Height && R.Height <= Range_MAX.Height)
                            {
                                Range = R;
                            }
                        }
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Cfg, "<BlockStyle>", "</BlockStyle>", false, false);

                        foreach (var V in Enum.GetValues(typeof(BlockStyles)))
                        {
                            if (SubStr.Trim().ToUpper() == V.ToString().ToUpper())
                            {
                                BlockStyle = (BlockStyles)V;

                                break;
                            }
                        }
                    }
                    catch { }

                    if (Com.Text.GetIntervalString(Cfg, "<ShowNumberOnImage>", "</ShowNumberOnImage>", false, false).Contains((!ShowNumberOnImage).ToString()))
                    {
                        ShowNumberOnImage = !ShowNumberOnImage;
                    }

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Cfg, "<Theme>", "</Theme>", false, false);

                        foreach (var V in Enum.GetValues(typeof(Com.WinForm.Theme)))
                        {
                            if (SubStr.Trim().ToUpper() == V.ToString().ToUpper())
                            {
                                Me.Theme = (Com.WinForm.Theme)V;

                                break;
                            }
                        }
                    }
                    catch { }

                    //

                    if (Com.Text.GetIntervalString(Cfg, "<UseRandomThemeColor>", "</UseRandomThemeColor>", false, false).Contains((!UseRandomThemeColor).ToString()))
                    {
                        UseRandomThemeColor = !UseRandomThemeColor;
                    }

                    if (!UseRandomThemeColor)
                    {
                        try
                        {
                            string SubStr = Com.Text.GetIntervalString(Cfg, "<ThemeColor>", "</ThemeColor>", false, false);

                            string[] Fields = SubStr.Split(',');

                            if (Fields.Length == 3)
                            {
                                int i = 0;

                                string StrR = RegexUint.Replace(Fields[i++], string.Empty);
                                Int32 TC_R = Convert.ToInt32(StrR);

                                string StrG = RegexUint.Replace(Fields[i++], string.Empty);
                                Int32 TC_G = Convert.ToInt32(StrG);

                                string StrB = RegexUint.Replace(Fields[i++], string.Empty);
                                Int32 TC_B = Convert.ToInt32(StrB);

                                Me.ThemeColor = Com.ColorX.FromRGB(TC_R, TC_G, TC_B);
                            }
                        }
                        catch { }
                    }

                    //

                    if (Com.Text.GetIntervalString(Cfg, "<ShowFormTitleColor>", "</ShowFormTitleColor>", false, false).Contains((!Me.ShowCaptionBarColor).ToString()))
                    {
                        Me.ShowCaptionBarColor = !Me.ShowCaptionBarColor;
                    }

                    //

                    try
                    {
                        string SubStr = RegexFloat.Replace(Com.Text.GetIntervalString(Cfg, "<Opacity>", "</Opacity>", false, false), string.Empty);

                        double Op = Convert.ToDouble(SubStr);

                        if (Op >= Opacity_MIN && Op <= Opacity_MAX)
                        {
                            Me.Opacity = Op;
                        }
                    }
                    catch { }

                    //

                    if (Com.Text.GetIntervalString(Cfg, "<AntiAlias>", "</AntiAlias>", false, false).Contains((!AntiAlias).ToString()))
                    {
                        AntiAlias = !AntiAlias;
                    }
                }
            }

            if (!LoadBkgImg())
            {
                BlockStyle = BlockStyles.Number;
            }
        }

        private void SaveConfig()
        {
            //
            // 保存配置文件。
            //

            string Cfg = string.Empty;

            Cfg += "<Config>";

            Cfg += "<ElementSize>" + ElementSize + "</ElementSize>";
            Cfg += "<Range>(" + Range.Width + "," + Range.Height + ")</Range>";
            Cfg += "<BlockStyle>" + BlockStyle + "</BlockStyle>";
            Cfg += "<ShowNumberOnImage>" + ShowNumberOnImage + "</ShowNumberOnImage>";

            Cfg += "<Theme>" + Me.Theme.ToString() + "</Theme>";
            Cfg += "<UseRandomThemeColor>" + UseRandomThemeColor + "</UseRandomThemeColor>";
            Cfg += "<ThemeColor>(" + Me.ThemeColor.ToColor().R + ", " + Me.ThemeColor.ToColor().G + ", " + Me.ThemeColor.ToColor().B + ")</ThemeColor>";
            Cfg += "<ShowFormTitleColor>" + Me.ShowCaptionBarColor + "</ShowFormTitleColor>";
            Cfg += "<Opacity>" + Me.Opacity + "</Opacity>";

            Cfg += "<AntiAlias>" + AntiAlias + "</AntiAlias>";

            Cfg += "</Config>";

            //

            try
            {
                if (!Directory.Exists(ConfigFileDir))
                {
                    Directory.CreateDirectory(ConfigFileDir);
                }

                StreamWriter Write = new StreamWriter(ConfigFilePath, false);
                Write.WriteLine(Cfg);
                Write.Close();
            }
            catch { }
        }

        #endregion

        #region 存档管理

        // 用户数据。

        private void LoadUserData()
        {
            //
            // 加载用户数据。
            //

            if (File.Exists(DataFilePath))
            {
                FileInfo FInfo = new FileInfo(DataFilePath);

                if (FInfo.Length > 0)
                {
                    StreamReader SR = new StreamReader(DataFilePath, false);
                    string Str = SR.ReadLine();
                    SR.Close();

                    Regex RegexUint = new Regex(@"[^0-9]");

                    //

                    try
                    {
                        string SubStr = RegexUint.Replace(Com.Text.GetIntervalString(Str, "<TotalGameTime>", "</TotalGameTime>", false, false), string.Empty);

                        TotalGameTime = TimeSpan.FromMilliseconds(Convert.ToInt64(SubStr));
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Str, "<BestRecord>", "</BestRecord>", false, false);

                        while (SubStr.IndexOf("(") != -1 && SubStr.IndexOf(")") != -1)
                        {
                            try
                            {
                                string StrRec = Com.Text.GetIntervalString(SubStr, "(", ")", false, false);

                                string[] Fields = StrRec.Split(',');

                                if (Fields.Length == 4)
                                {
                                    int i = 0;

                                    Record Rec = new Record();

                                    string StrRW = RegexUint.Replace(Fields[i++], string.Empty);
                                    Rec.Range.Width = Convert.ToInt32(StrRW);

                                    string StrRH = RegexUint.Replace(Fields[i++], string.Empty);
                                    Rec.Range.Height = Convert.ToInt32(StrRH);

                                    string StrTS = RegexUint.Replace(Fields[i++], string.Empty);
                                    Rec.GameTime = TimeSpan.FromMilliseconds(Convert.ToInt64(StrTS));

                                    string StrSC = RegexUint.Replace(Fields[i++], string.Empty);
                                    Rec.StepCount = Convert.ToInt32(StrSC);

                                    if ((Rec.Range.Width >= Range_MIN.Width && Rec.Range.Width <= Range_MAX.Width) && (Rec.Range.Height >= Range_MIN.Height && Rec.Range.Height <= Range_MAX.Height))
                                    {
                                        BestRecordArray[Rec.Range.Width - Range_MIN.Width, Rec.Range.Height - Range_MIN.Height] = Rec;
                                    }
                                }
                            }
                            catch { }

                            SubStr = SubStr.Substring(SubStr.IndexOf(")") + (")").Length);
                        }
                    }
                    catch { }
                }
            }
        }

        private void SaveUserData()
        {
            //
            // 保存用户数据。
            //

            if (GameIsWin && (BestRecord.GameTime.TotalMilliseconds == 0 || (ThisRecord.GameTime.TotalMilliseconds > 0 && BestRecord.GameTime > ThisRecord.GameTime)) && ThisRecord.StepCount > 0)
            {
                BestRecord = ThisRecord;
            }

            //

            string Str = string.Empty;

            Str += "<Log>";

            Str += "<TotalGameTime>" + (Int64)TotalGameTime.TotalMilliseconds + "</TotalGameTime>";

            Str += "<BestRecord>[";
            for (int w = Range_MIN.Width; w <= Range_MAX.Width; w++)
            {
                for (int h = Range_MIN.Height; h <= Range_MAX.Height; h++)
                {
                    Record Rec = BestRecordArray[w - Range_MIN.Width, h - Range_MIN.Height];

                    if (Rec.GameTime.TotalMilliseconds > 0 && Rec.StepCount > 0)
                    {
                        Str += "(" + Rec.Range.Width + "," + Rec.Range.Height + "," + Rec.GameTime.TotalMilliseconds + "," + Rec.StepCount + ")";
                    }
                }
            }
            Str += "]</BestRecord>";

            Str += "</Log>";

            //

            try
            {
                if (!Directory.Exists(LogFileDir))
                {
                    Directory.CreateDirectory(LogFileDir);
                }

                StreamWriter SW = new StreamWriter(DataFilePath, false);
                SW.WriteLine(Str);
                SW.Close();
            }
            catch { }
        }

        // 上次游戏。

        private void LoadLastGame()
        {
            //
            // 加载上次游戏。
            //

            if (File.Exists(RecordFilePath))
            {
                FileInfo FInfo = new FileInfo(RecordFilePath);

                if (FInfo.Length > 0)
                {
                    StreamReader SR = new StreamReader(RecordFilePath, false);
                    string Str = SR.ReadLine();
                    SR.Close();

                    Regex RegexUint = new Regex(@"[^0-9]");

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Str, "<Range>", "</Range>", false, false);

                        string[] Fields = SubStr.Split(',');

                        if (Fields.Length == 2)
                        {
                            int i = 0;

                            string StrW = RegexUint.Replace(Fields[i++], string.Empty);
                            string StrH = RegexUint.Replace(Fields[i++], string.Empty);

                            Size R = new Size(Convert.ToInt32(StrW), Convert.ToInt32(StrH));

                            if ((R.Width >= Range_MIN.Width && R.Width <= Range_MAX.Width) && (R.Height >= Range_MIN.Height && R.Height <= Range_MAX.Height))
                            {
                                Record_Last.Range = R;
                            }
                        }
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Str, "<Element>", "</Element>", false, false);

                        while (SubStr.IndexOf("(") != -1 && SubStr.IndexOf(")") != -1)
                        {
                            try
                            {
                                string StrE = Com.Text.GetIntervalString(SubStr, "(", ")", false, false);

                                string[] Fields = StrE.Split(',');

                                if (Fields.Length == 3)
                                {
                                    int i = 0;

                                    Point Index = new Point();
                                    Int32 E = 0;

                                    string StrIDX = RegexUint.Replace(Fields[i++], string.Empty);
                                    Index.X = Convert.ToInt32(StrIDX);

                                    string StrIDY = RegexUint.Replace(Fields[i++], string.Empty);
                                    Index.Y = Convert.ToInt32(StrIDY);

                                    string StrVal = RegexUint.Replace(Fields[i++], string.Empty);
                                    E = Convert.ToInt32(StrVal);

                                    if ((Index.X >= 0 && Index.X < Record_Last.Range.Width && Index.Y >= 0 && Index.Y < Record_Last.Range.Height) && (E >= 1 && E < Record_Last.Range.Width * Record_Last.Range.Height))
                                    {
                                        ElementArray_Last[Index.X, Index.Y] = E;
                                        ElementIndexList_Last.Add(Index);
                                    }
                                }
                            }
                            catch { }

                            SubStr = SubStr.Substring(SubStr.IndexOf(")") + (")").Length);
                        }
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = RegexUint.Replace(Com.Text.GetIntervalString(Str, "<GameTime>", "</GameTime>", false, false), string.Empty);

                        Record_Last.GameTime = TimeSpan.FromMilliseconds(Convert.ToInt64(SubStr));
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = RegexUint.Replace(Com.Text.GetIntervalString(Str, "<StepCount>", "</StepCount>", false, false), string.Empty);

                        Record_Last.StepCount = Convert.ToInt32(SubStr);
                    }
                    catch { }
                }
            }
        }

        private void SaveLastGame()
        {
            //
            // 保存上次游戏。
            //

            Record_Last = ThisRecord;

            foreach (var V in ElementIndexList_Last)
            {
                ElementArray_Last[V.X, V.Y] = 0;
            }

            ElementIndexList_Last.Clear();

            foreach (var V in ElementIndexList)
            {
                ElementArray_Last[V.X, V.Y] = ElementArray[V.X, V.Y];

                ElementIndexList_Last.Add(V);
            }

            //

            string Str = string.Empty;

            Str += "<Log>";

            Str += "<Range>(" + Range.Width + "," + Range.Height + ")</Range>";

            Str += "<Element>[";
            for (int i = 0; i < ElementIndexList.Count; i++)
            {
                Point A = ElementIndexList[i];

                Str += "(" + A.X + "," + A.Y + "," + ElementArray[A.X, A.Y] + ")";
            }
            Str += "]</Element>";

            Str += "<GameTime>" + ThisRecord.GameTime.TotalMilliseconds + "</GameTime>";
            Str += "<StepCount>" + ThisRecord.StepCount + "</StepCount>";

            Str += "</Log>";

            //

            try
            {
                if (!Directory.Exists(LogFileDir))
                {
                    Directory.CreateDirectory(LogFileDir);
                }

                StreamWriter SW = new StreamWriter(RecordFilePath, false);
                SW.WriteLine(Str);
                SW.Close();
            }
            catch { }
        }

        private void EraseLastGame()
        {
            //
            // 擦除上次游戏。
            //

            foreach (var V in ElementIndexList_Last)
            {
                ElementArray_Last[V.X, V.Y] = 0;
            }

            ElementIndexList_Last.Clear();

            Record_Last = new Record();

            //

            try
            {
                if (!Directory.Exists(LogFileDir))
                {
                    Directory.CreateDirectory(LogFileDir);
                }

                StreamWriter SW = new StreamWriter(RecordFilePath, false);
                SW.WriteLine(string.Empty);
                SW.Close();
            }
            catch { }
        }

        #endregion

        #region 数组功能

        // 拷贝。

        private Int32[,] GetCopyOfArray(Int32[,] Array)
        {
            //
            // 返回二维矩阵的浅表副本。Array：矩阵。
            //

            return (Int32[,])Array.Clone();
        }

        // 冗余量。

        private Int32 GetZeroCountOfArray(Int32[,] Array, Size Cap)
        {
            //
            // 计算二维矩阵值为 0 的元素的数量。Array：矩阵，索引为 [x, y]；Cap：矩阵的大小，分量 (Width, Height) 分别表示沿 x 方向和沿 y 方向的元素数量。
            //

            try
            {
                Int32 ZeroCount = 0;

                for (int X = 0; X < Cap.Width; X++)
                {
                    for (int Y = 0; Y < Cap.Height; Y++)
                    {
                        if (Array[X, Y] == 0)
                        {
                            ZeroCount++;
                        }
                    }
                }

                return ZeroCount;
            }
            catch
            {
                return 0;
            }
        }

        // 统计。

        private List<Point> GetCertainIndexListOfArray(Int32[,] Array, Size Cap, Int32 Value)
        {
            //
            // 返回二维矩阵中所有值为指定值的元素的索引的列表。Array：矩阵，索引为 [x, y]；Cap：矩阵的大小，分量 (Width, Height) 分别表示沿 x 方向和沿 y 方向的元素数量；Value：指定值。
            //

            List<Point> L = new List<Point>(0);

            try
            {
                for (int X = 0; X < Cap.Width; X++)
                {
                    for (int Y = 0; Y < Cap.Height; Y++)
                    {
                        if (Array[X, Y] == Value)
                        {
                            L.Add(new Point(X, Y));
                        }
                    }
                }

                return L;
            }
            catch
            {
                return L;
            }
        }

        #endregion

        #region 元素矩阵基本功能

        // 初始化。

        private void ElementArray_Initialize()
        {
            //
            // 初始化元素矩阵。
            //

            foreach (var V in ElementIndexList)
            {
                ElementArray[V.X, V.Y] = 0;
            }

            ElementIndexList.Clear();
        }

        // 索引。

        private Point ElementArray_GetIndex(Point P)
        {
            //
            // 获取绘图容器中的指定坐标所在元素的索引。P：坐标。
            //

            try
            {
                Point dP = new Point(P.X - EAryBmpRect.X, P.Y - EAryBmpRect.Y);
                Point A = new Point((Int32)Math.Floor((double)dP.X / ElementSize), (Int32)Math.Floor((double)dP.Y / ElementSize));

                if (A.X >= 0 && A.X < Range.Width && A.Y >= 0 && A.Y < Range.Height)
                {
                    return A;
                }

                return new Point(-1, -1);
            }
            catch
            {
                return new Point(-1, -1);
            }
        }

        // 颜色。

        private Color ElementArray_GetColor(Int32 E)
        {
            //
            // 获取元素颜色。E：元素的值。
            //

            try
            {
                if (E == 0)
                {
                    return Me.RecommendColors.Background.ToColor();
                }
                else if (E >= 1)
                {
                    return Me.RecommendColors.Main_DEC.ToColor();
                }

                return Color.Empty;
            }
            catch
            {
                return Color.Empty;
            }
        }

        // 添加与移除。

        private void ElementArray_Add(Point A, Int32 E)
        {
            //
            // 向元素矩阵添加一个元素。A：索引；E：元素的值。
            //

            if (E != 0 && A.X >= 0 && A.X < CAPACITY && A.Y >= 0 && A.Y < CAPACITY)
            {
                if (!ElementIndexList.Contains(A))
                {
                    ElementArray[A.X, A.Y] = E;

                    ElementIndexList.Add(A);
                }
            }
        }

        private void ElementArray_RemoveAt(Point A)
        {
            //
            // 从元素矩阵移除一个元素。A：索引。
            //

            if (A.X >= 0 && A.X < CAPACITY && A.Y >= 0 && A.Y < CAPACITY)
            {
                ElementArray[A.X, A.Y] = 0;

                if (ElementIndexList.Contains(A))
                {
                    ElementIndexList.Remove(A);
                }
            }
        }

        // 绘图与呈现。

        private Rectangle EAryBmpRect = new Rectangle(); // 元素矩阵位图区域（相对于绘图容器）。

        private Bitmap EAryBmp; // 元素矩阵位图。

        private Graphics EAryBmpGrap; // 元素矩阵位图绘图。

        private void ElementArray_DrawInRectangle(Int32 E, Rectangle Rect, bool PresentNow)
        {
            //
            // 在元素矩阵位图的指矩形区域内绘制一个元素。E：元素的值；Rect：矩形区域；PresentNow：是否立即呈现此元素，如果为 true，那么将在位图中绘制此元素，并在不重绘整个位图的情况下在容器中绘制此元素，如果为 false，那么将仅在位图中绘制此元素。
            //

            Rectangle BmpRect = new Rectangle(new Point(Rect.X - (ElementSize - Rect.Width) / 2, Rect.Y - (ElementSize - Rect.Height) / 2), new Size(ElementSize, ElementSize));

            Bitmap Bmp = new Bitmap(BmpRect.Width, BmpRect.Height);

            Graphics BmpGrap = Graphics.FromImage(Bmp);

            if (AntiAlias)
            {
                BmpGrap.SmoothingMode = SmoothingMode.AntiAlias;
                BmpGrap.TextRenderingHint = TextRenderingHint.AntiAlias;
            }

            //

            const double ElementClientDistPct = 1.0 / 12.0; // 相邻两元素有效区域的间距与元素边长之比。

            if (Rect.Width < BmpRect.Width || Rect.Height < BmpRect.Height)
            {
                Rectangle Rect_Bkg = new Rectangle(new Point((Int32)(ElementSize * ElementClientDistPct / 2), (Int32)(ElementSize * ElementClientDistPct / 2)), new Size((Int32)(Math.Max(1, ElementSize * (1 - ElementClientDistPct))), (Int32)(Math.Max(1, ElementSize * (1 - ElementClientDistPct)))));

                GraphicsPath RndRect_Bkg = Com.Geometry.CreateRoundedRectanglePath(Rect_Bkg, (Int32)(ElementSize * ElementClientDistPct / 2));

                Color Cr_Bkg = (E > 0 ? ElementArray_GetColor(0) : Color.FromArgb((Int32)(Math.Max(0, Math.Min(1, (double)(Rect.Width * Rect.Height) / (BmpRect.Width * BmpRect.Height))) * 255), ElementArray_GetColor(0)));

                if (!Timer_Timer.Enabled && !GameIsWin)
                {
                    Cr_Bkg = Com.ColorManipulation.GetGrayscaleColor(Cr_Bkg);
                }

                BmpGrap.FillPath(new LinearGradientBrush(new Point(Rect_Bkg.X - 1, Rect_Bkg.Y - 1), new Point(Rect_Bkg.Right, Rect_Bkg.Bottom), Com.ColorManipulation.ShiftLightnessByHSL(Cr_Bkg, 0.3), Cr_Bkg), RndRect_Bkg);
            }

            Rectangle Rect_Cen = new Rectangle(new Point((Int32)((ElementSize - Rect.Width) / 2 + Rect.Width * ElementClientDistPct / 2), (Int32)((ElementSize - Rect.Height) / 2 + Rect.Height * ElementClientDistPct / 2)), new Size((Int32)(Math.Max(1, Rect.Width * (1 - ElementClientDistPct))), (Int32)(Math.Max(1, Rect.Height * (1 - ElementClientDistPct)))));

            GraphicsPath RndRect_Cen = Com.Geometry.CreateRoundedRectanglePath(Rect_Cen, (Int32)(ElementSize * ElementClientDistPct / 2));

            Color Cr_Cen = Color.FromArgb((Int32)(Math.Max(0, Math.Min(1, (double)(Rect.Width * Rect.Height) / (BmpRect.Width * BmpRect.Height))) * 255), ElementArray_GetColor(E));

            if (!Timer_Timer.Enabled && !GameIsWin)
            {
                Cr_Cen = Com.ColorManipulation.GetGrayscaleColor(Cr_Cen);
            }

            if (BlockStyle == BlockStyles.Image)
            {
                Bitmap SubImg = ElementArray_GetBackgroundImage(E);

                if (SubImg != null)
                {
                    BmpGrap.DrawImage(ElementArray_GetBackgroundImage(E), Rect_Cen);

                    GraphicsPath[] RndRect_Cen_Otr = Com.Geometry.CreateRoundedRectangleOuterPaths(Rect_Cen, (Int32)(ElementSize * ElementClientDistPct / 2));

                    foreach (var V in RndRect_Cen_Otr)
                    {
                        BmpGrap.FillPath(new SolidBrush(GameUIBackColor_INC), V);
                    }
                }
                else
                {
                    BmpGrap.FillPath(new LinearGradientBrush(new Point(Rect_Cen.X - 1, Rect_Cen.Y - 1), new Point(Rect_Cen.Right, Rect_Cen.Bottom), Com.ColorManipulation.ShiftLightnessByHSL(Cr_Cen, 0.3), Cr_Cen), RndRect_Cen);
                }
            }
            else
            {
                BmpGrap.FillPath(new LinearGradientBrush(new Point(Rect_Cen.X - 1, Rect_Cen.Y - 1), new Point(Rect_Cen.Right, Rect_Cen.Bottom), Com.ColorManipulation.ShiftLightnessByHSL(Cr_Cen, 0.3), Cr_Cen), RndRect_Cen);
            }

            //

            if (BlockStyle == BlockStyles.Number || (BlockStyle == BlockStyles.Image && ShowNumberOnImage))
            {
                string StringText = (E >= 1 ? E.ToString() : string.Empty);

                if (StringText.Length > 0)
                {
                    Color StringColor_Fore = GameUIBackColor_DEC, StringColor_Back = Color.Black;
                    Font StringFont;
                    RectangleF StringRect = new RectangleF();

                    if (BlockStyle == BlockStyles.Image)
                    {
                        StringFont = Com.Text.GetSuitableFont(StringText, new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SizeF(Rect_Cen.Width * 0.25F, Rect_Cen.Height * 0.25F));
                        StringRect = new RectangleF(new PointF(Rect_Cen.X + Rect_Cen.Width * 0.05F, Rect_Cen.Y + Rect_Cen.Height * 0.05F), BmpGrap.MeasureString(StringText, StringFont));
                    }
                    else
                    {
                        StringFont = Com.Text.GetSuitableFont(StringText, new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SizeF(Rect_Cen.Width * 0.8F, Rect_Cen.Height * 0.8F));
                        StringRect.Size = BmpGrap.MeasureString(StringText, StringFont);
                        StringRect.Location = new PointF(Rect_Cen.X + (Rect_Cen.Width - StringRect.Width) / 2, Rect_Cen.Y + (Rect_Cen.Height - StringRect.Height) / 2);
                    }

                    Com.Painting2D.PaintTextWithShadow(Bmp, StringText, StringFont, StringColor_Fore, StringColor_Back, StringRect.Location, 0.02F, AntiAlias);
                }
            }

            //

            if (Bmp != null)
            {
                EAryBmpGrap.DrawImage(Bmp, BmpRect.Location);

                if (PresentNow)
                {
                    Panel_Environment.CreateGraphics().DrawImage(Bmp, new Point(EAryBmpRect.X + BmpRect.X, EAryBmpRect.Y + BmpRect.Y));
                }
            }
        }

        private void ElementArray_RepresentAll()
        {
            //
            // 更新并呈现元素矩阵包含的所有元素。
            //

            if (Panel_Environment.Visible && (Panel_Environment.Width > 0 && Panel_Environment.Height > 0))
            {
                if (EAryBmp != null)
                {
                    EAryBmp.Dispose();
                }

                EAryBmp = new Bitmap(Math.Max(1, EAryBmpRect.Width), Math.Max(1, EAryBmpRect.Height));

                EAryBmpGrap = Graphics.FromImage(EAryBmp);

                if (AntiAlias)
                {
                    EAryBmpGrap.SmoothingMode = SmoothingMode.AntiAlias;
                    EAryBmpGrap.TextRenderingHint = TextRenderingHint.AntiAlias;
                }

                EAryBmpGrap.Clear(GameUIBackColor_INC);

                //

                for (int X = 0; X < Range.Width; X++)
                {
                    for (int Y = 0; Y < Range.Height; Y++)
                    {
                        Int32 E = ElementArray[X, Y];

                        Rectangle Rect = new Rectangle(new Point(X * ElementSize, Y * ElementSize), new Size(ElementSize, ElementSize));

                        ElementArray_DrawInRectangle(E, Rect, false);
                    }
                }

                //

                if (!Timer_Timer.Enabled)
                {
                    EAryBmpGrap.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.White)), new Rectangle(new Point(0, 0), EAryBmp.Size));

                    //

                    string StringText = string.Empty;
                    Color StringColor = Me.RecommendColors.Text.ToColor();

                    if (GameIsWin)
                    {
                        StringText = "成功";
                    }
                    else
                    {
                        StringText = "已暂停";
                    }

                    Font StringFont = Com.Text.GetSuitableFont(StringText, new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SizeF(EAryBmp.Width * 0.8F, EAryBmp.Height * 0.2F));
                    RectangleF StringRect = new RectangleF();
                    StringRect.Size = EAryBmpGrap.MeasureString(StringText, StringFont);
                    StringRect.Location = new PointF((EAryBmp.Width - StringRect.Width) / 2, (EAryBmp.Height - StringRect.Height) / 2);

                    Color StringBkColor = Com.ColorManipulation.ShiftLightnessByHSL(StringColor, 0.5);
                    Rectangle StringBkRect = new Rectangle(new Point(0, (Int32)StringRect.Y), new Size(EAryBmp.Width, Math.Max(1, (Int32)StringRect.Height)));

                    GraphicsPath Path_StringBk = new GraphicsPath();
                    Path_StringBk.AddRectangle(StringBkRect);
                    PathGradientBrush PGB_StringBk = new PathGradientBrush(Path_StringBk)
                    {
                        CenterColor = Color.FromArgb(192, StringBkColor),
                        SurroundColors = new Color[] { Color.Transparent },
                        FocusScales = new PointF(0F, 1F)
                    };
                    EAryBmpGrap.FillPath(PGB_StringBk, Path_StringBk);
                    Path_StringBk.Dispose();
                    PGB_StringBk.Dispose();

                    Com.Painting2D.PaintTextWithShadow(EAryBmp, StringText, StringFont, StringColor, StringColor, StringRect.Location, 0.02F, AntiAlias);
                }

                //

                RepaintEAryBmp();
            }
        }

        private void ElementArray_AnimatePresentAt(Point A)
        {
            //
            // 以动画效果呈现元素矩阵中指定的索引处的一个元素。A：索引。
            //

            if (Panel_Environment.Visible && (Panel_Environment.Width > 0 && Panel_Environment.Height > 0))
            {
                if (A.X >= 0 && A.X < Range.Width && A.Y >= 0 && A.Y < Range.Height)
                {
                    Int32 E = ElementArray[A.X, A.Y];

                    Com.Animation.Frame Frame = (frameId, frameCount, msPerFrame) =>
                    {
                        double Pct_F = (frameId == frameCount ? 1 : 1 - Math.Pow(1 - (double)frameId / frameCount, 2));

                        Int32 RectSize = (Int32)Math.Max(1, ElementSize * Pct_F);

                        Rectangle Rect = new Rectangle(new Point(A.X * ElementSize + (ElementSize - RectSize) / 2, A.Y * ElementSize + (ElementSize - RectSize) / 2), new Size(RectSize, RectSize));

                        ElementArray_DrawInRectangle(E, Rect, false);

                        RepaintEAryBmp();
                    };

                    Com.Animation.Show(Frame, 12, 15);
                }
            }
        }

        private void ElementArray_AnimatePresentAt(List<Point> A, bool ClearBmp)
        {
            //
            // 以动画效果同时呈现元素矩阵中由索引数组指定的所有元素。A：索引列表；ClearBmp：在呈现之前是否首先清除绘图。
            //

            if (Panel_Environment.Visible && (Panel_Environment.Width > 0 && Panel_Environment.Height > 0))
            {
                if (ClearBmp)
                {
                    EAryBmpGrap.Clear(GameUIBackColor_INC);

                    for (int X = 0; X < Range.Width; X++)
                    {
                        for (int Y = 0; Y < Range.Height; Y++)
                        {
                            Rectangle Rect = new Rectangle(new Point(X * ElementSize, Y * ElementSize), new Size(ElementSize, ElementSize));

                            ElementArray_DrawInRectangle(0, Rect, false);
                        }
                    }
                }

                Com.Animation.Frame Frame = (frameId, frameCount, msPerFrame) =>
                {
                    double Pct_F = (frameId == frameCount ? 1 : 1 - Math.Pow(1 - (double)frameId / frameCount, 2));

                    Int32 RectSize = (Int32)Math.Max(1, ElementSize * Pct_F);

                    foreach (var V in A)
                    {
                        if (V.X >= 0 && V.X < Range.Width && V.Y >= 0 && V.Y < Range.Height)
                        {
                            Int32 E = ElementArray[V.X, V.Y];

                            Rectangle Rect = new Rectangle(new Point(V.X * ElementSize + (ElementSize - RectSize) / 2, V.Y * ElementSize + (ElementSize - RectSize) / 2), new Size(RectSize, RectSize));

                            ElementArray_DrawInRectangle(E, Rect, false);
                        }
                    }

                    RepaintEAryBmp();
                };

                Com.Animation.Show(Frame, 12, 15);
            }
        }

        private void ElementArray_AnimateMove(Point[,] OldIndex)
        {
            //
            // 以动画效果呈现元素矩阵中若干元素的平移。OldIndex：元素矩阵中所有元素在平移之前的索引。
            //

            if (Panel_Environment.Visible && (Panel_Environment.Width > 0 && Panel_Environment.Height > 0))
            {
                Com.Animation.Frame FrameA = (frameId, frameCount, msPerFrame) =>
                {
                    EAryBmpGrap.Clear(GameUIBackColor_INC);

                    for (int X = 0; X < Range.Width; X++)
                    {
                        for (int Y = 0; Y < Range.Height; Y++)
                        {
                            Int32 E = ElementArray[X, Y];

                            Rectangle Rect = new Rectangle(new Point(X * ElementSize, Y * ElementSize), new Size(ElementSize, ElementSize));

                            if (E != 0 && OldIndex[X, Y] != new Point(X, Y))
                            {
                                ElementArray_DrawInRectangle(0, Rect, false);
                            }
                            else
                            {
                                ElementArray_DrawInRectangle(E, Rect, false);
                            }
                        }
                    }

                    for (int X = 0; X < Range.Width; X++)
                    {
                        for (int Y = 0; Y < Range.Height; Y++)
                        {
                            Int32 E = ElementArray[X, Y];

                            if (E != 0 && OldIndex[X, Y] != new Point(X, Y))
                            {
                                double N = (frameId == frameCount ? 0 : 1 - Math.Pow((double)frameId / frameCount, 2));

                                Rectangle Rect = new Rectangle(new Point((Int32)((X * (1 - N) + OldIndex[X, Y].X * N) * ElementSize), (Int32)((Y * (1 - N) + OldIndex[X, Y].Y * N) * ElementSize)), new Size(ElementSize, ElementSize));

                                ElementArray_DrawInRectangle(E, Rect, false);
                            }
                        }
                    }

                    RepaintEAryBmp();
                };

                Com.Animation.Show(FrameA, 9, 15);

                Com.Animation.Frame FrameB = (frameId, frameCount, msPerFrame) =>
                {
                    EAryBmpGrap.Clear(GameUIBackColor_INC);

                    for (int X = 0; X < Range.Width; X++)
                    {
                        for (int Y = 0; Y < Range.Height; Y++)
                        {
                            Int32 E = ElementArray[X, Y];

                            Rectangle Rect = new Rectangle(new Point(X * ElementSize, Y * ElementSize), new Size(ElementSize, ElementSize));

                            if (E != 0 && OldIndex[X, Y] != new Point(X, Y))
                            {
                                ElementArray_DrawInRectangle(0, Rect, false);
                            }
                            else
                            {
                                ElementArray_DrawInRectangle(E, Rect, false);
                            }
                        }
                    }

                    for (int X = 0; X < Range.Width; X++)
                    {
                        for (int Y = 0; Y < Range.Height; Y++)
                        {
                            Int32 E = ElementArray[X, Y];

                            if (E != 0 && OldIndex[X, Y] != new Point(X, Y))
                            {
                                double N = (frameId == frameCount ? 0 : 0.1 * (1 - Math.Pow((frameId - frameCount * 0.4) / (frameCount * 0.6), 2)));

                                Rectangle Rect = new Rectangle(new Point((Int32)((X + Math.Sign(OldIndex[X, Y].X - X) * N) * ElementSize), (Int32)((Y + Math.Sign(OldIndex[X, Y].Y - Y) * N) * ElementSize)), new Size(ElementSize, ElementSize));

                                ElementArray_DrawInRectangle(E, Rect, false);
                            }
                        }
                    }

                    RepaintEAryBmp();
                };

                Com.Animation.Show(FrameB, 5, 15);

                ElementArray_RepresentAll();
            }
        }

        private void RepaintEAryBmp()
        {
            //
            // 重绘元素矩阵位图。
            //

            if (EAryBmp != null)
            {
                if (Panel_Environment.Width > EAryBmp.Width)
                {
                    Panel_Environment.CreateGraphics().FillRectangles(new SolidBrush(GameUIBackColor_DEC), new Rectangle[] { new Rectangle(new Point(0, 0), new Size((Panel_Environment.Width - EAryBmp.Width) / 2, Panel_Environment.Height)), new Rectangle(new Point(Panel_Environment.Width - (Panel_Environment.Width - EAryBmp.Width) / 2, 0), new Size((Panel_Environment.Width - EAryBmp.Width) / 2, Panel_Environment.Height)) });
                }

                if (Panel_Environment.Height > EAryBmp.Height)
                {
                    Panel_Environment.CreateGraphics().FillRectangles(new SolidBrush(GameUIBackColor_DEC), new Rectangle[] { new Rectangle(new Point(0, 0), new Size(Panel_Environment.Width, (Panel_Environment.Height - EAryBmp.Height) / 2)), new Rectangle(new Point(0, Panel_Environment.Height - (Panel_Environment.Height - EAryBmp.Height) / 2), new Size(Panel_Environment.Width, (Panel_Environment.Height - EAryBmp.Height) / 2)) });
                }

                Panel_Environment.CreateGraphics().DrawImage(EAryBmp, EAryBmpRect);
            }
        }

        private void Panel_Environment_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_Environment 绘图。
            //

            if (EAryBmp != null)
            {
                if (Panel_Environment.Width > EAryBmp.Width)
                {
                    e.Graphics.FillRectangles(new SolidBrush(GameUIBackColor_DEC), new Rectangle[] { new Rectangle(new Point(0, 0), new Size((Panel_Environment.Width - EAryBmp.Width) / 2, Panel_Environment.Height)), new Rectangle(new Point(Panel_Environment.Width - (Panel_Environment.Width - EAryBmp.Width) / 2, 0), new Size((Panel_Environment.Width - EAryBmp.Width) / 2, Panel_Environment.Height)) });
                }

                if (Panel_Environment.Height > EAryBmp.Height)
                {
                    e.Graphics.FillRectangles(new SolidBrush(GameUIBackColor_DEC), new Rectangle[] { new Rectangle(new Point(0, 0), new Size(Panel_Environment.Width, (Panel_Environment.Height - EAryBmp.Height) / 2)), new Rectangle(new Point(0, Panel_Environment.Height - (Panel_Environment.Height - EAryBmp.Height) / 2), new Size(Panel_Environment.Width, (Panel_Environment.Height - EAryBmp.Height) / 2)) });
                }

                e.Graphics.DrawImage(EAryBmp, EAryBmpRect);
            }
        }

        #endregion

        #region 元素矩阵高级功能

        // 背景图片。

        private Bitmap ElementArray_GetBackgroundImage(Int32 E)
        {
            //
            // 获取元素背景图片。E：元素的值。
            //

            try
            {
                if (E == 0)
                {
                    if (GameIsWin)
                    {
                        Point A = GetCorrectIndexFromValue(E);

                        return SubImgAry[A.X, A.Y];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    Point A = GetCorrectIndexFromValue(E);

                    if (!Timer_Timer.Enabled && !GameIsWin)
                    {
                        return SubImgAry_Gray[A.X, A.Y];
                    }
                    else
                    {
                        return SubImgAry[A.X, A.Y];
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        // 逻辑平移与添加。

        private void _ElementArray_LogicalMove(Point A, Point Az)
        {
            //
            // 将元素矩阵自索引 A 向索引 Az 方向进行逻辑平移（不附加任何其他操作，不保证操作本身与操作结果的合法性）。A，Az：索引。
            //

            if (A.Y == Az.Y)
            {
                if (A.X < Az.X)
                {
                    ElementArray_Add(Az, ElementArray[Az.X - 1, Az.Y]);

                    for (int X = Az.X - 1; X >= A.X + 1; X--)
                    {
                        ElementArray[X, Az.Y] = ElementArray[X - 1, Az.Y];
                    }
                }
                else if (A.X > Az.X)
                {
                    ElementArray_Add(Az, ElementArray[Az.X + 1, Az.Y]);

                    for (int X = Az.X + 1; X < A.X; X++)
                    {
                        ElementArray[X, Az.Y] = ElementArray[X + 1, Az.Y];
                    }
                }
            }
            else if (A.X == Az.X)
            {
                if (A.Y < Az.Y)
                {
                    ElementArray_Add(Az, ElementArray[Az.X, Az.Y - 1]);

                    for (int Y = Az.Y - 1; Y >= A.Y + 1; Y--)
                    {
                        ElementArray[Az.X, Y] = ElementArray[Az.X, Y - 1];
                    }
                }
                else if (A.Y > Az.Y)
                {
                    ElementArray_Add(Az, ElementArray[Az.X, Az.Y + 1]);

                    for (int Y = Az.Y + 1; Y < A.Y; Y++)
                    {
                        ElementArray[Az.X, Y] = ElementArray[Az.X, Y + 1];
                    }
                }
            }

            ElementArray_RemoveAt(A);
        }

        private bool ElementArray_LogicalMove(Point A)
        {
            //
            // 将元素矩阵自指定索引向唯一可能的方向进行逻辑平移，并返回此操作之后的元素矩阵是否与之前的元素矩阵相同，如果不相同，以动画效果呈现此过程。A：索引。
            //

            try
            {
                Int32[,] OriginalElementArray = GetCopyOfArray(ElementArray);

                //

                List<Point> L_Az = GetCertainIndexListOfArray(ElementArray, Range, 0);

                if (L_Az.Count == 1)
                {
                    Point Az = L_Az[0];

                    if ((A.X == Az.X || A.Y == Az.Y) && A != Az)
                    {
                        _ElementArray_LogicalMove(A, Az);

                        ThisRecord.StepCount += 1;
                    }

                    //

                    bool Flag = true;

                    for (int X = 0; X < Range.Width; X++)
                    {
                        for (int Y = 0; Y < Range.Height; Y++)
                        {
                            if (ElementArray[X, Y] != OriginalElementArray[X, Y])
                            {
                                Flag = false;

                                break;
                            }
                        }
                    }

                    //

                    if (!Flag)
                    {
                        Point[,] OldIndex = new Point[Range.Width, Range.Height];

                        for (int X = 0; X < Range.Width; X++)
                        {
                            for (int Y = 0; Y < Range.Height; Y++)
                            {
                                OldIndex[X, Y] = new Point(X, Y);
                            }
                        }

                        if (A.Y == Az.Y)
                        {
                            if (A.X < Az.X)
                            {
                                for (int X = Az.X; X >= A.X + 1; X--)
                                {
                                    OldIndex[X, Az.Y] = new Point(X - 1, Az.Y);
                                }
                            }
                            else if (A.X > Az.X)
                            {
                                for (int X = Az.X; X < A.X; X++)
                                {
                                    OldIndex[X, Az.Y] = new Point(X + 1, Az.Y);
                                }
                            }
                        }
                        else if (A.X == Az.X)
                        {
                            if (A.Y < Az.Y)
                            {
                                for (int Y = Az.Y; Y >= A.Y + 1; Y--)
                                {
                                    OldIndex[Az.X, Y] = new Point(Az.X, Y - 1);
                                }
                            }
                            else if (A.Y > Az.Y)
                            {
                                for (int Y = Az.Y; Y < A.Y; Y++)
                                {
                                    OldIndex[Az.X, Y] = new Point(Az.X, Y + 1);
                                }
                            }
                        }

                        //

                        ElementArray_AnimateMove(OldIndex);

                        //

                        Judgement();
                    }

                    //

                    return Flag;
                }

                //

                return true;
            }
            catch
            {
                return true;
            }
        }

        // 元素值与索引的正确映射。

        private Int32 GetCorrectValueFromIndex(Point A)
        {
            //
            // 获取指定索引处的正确元素值。A：索引。
            //

            try
            {
                if (A.X == Range.Width - 1 && A.Y == Range.Height - 1)
                {
                    return 0;
                }

                return (A.Y * Range.Width + A.X + 1);
            }
            catch
            {
                return -1;
            }
        }

        private Point GetCorrectIndexFromValue(Int32 E)
        {
            //
            // 获取指定元素值的正确索引。A：索引。
            //

            try
            {
                if (E == 0)
                {
                    return new Point(Range.Width - 1, Range.Height - 1);
                }

                return new Point((E - 1) % Range.Width, (E - 1) / Range.Width);
            }
            catch
            {
                return new Point(-1, -1);
            }
        }

        private Int32 CorrectTileCount // 当前拼图板已归位的区块数（元素矩阵中元素值与索引映射正确的数量）（不计入零值元素）。
        {
            get
            {
                Int32 Count = 0;

                foreach (var V in ElementIndexList)
                {
                    if (ElementArray[V.X, V.Y] == GetCorrectValueFromIndex(V))
                    {
                        Count += 1;
                    }
                }

                return Count;
            }
        }

        private Int32 TotalTileCount => (Range.Width * Range.Height - 1); // 当前布局的拼图板中的区块总数（元素矩阵中非零值元素的数量）（以拼图板合法为前提）。

        // 生成与打乱。

        private void CreateMap()
        {
            //
            // 生成新的拼图板（需首先初始化元素矩阵）。
            //

            for (int i = 1; i <= TotalTileCount; i++)
            {
                ElementArray_Add(GetCorrectIndexFromValue(i), i);
            }
        }

        private void DisruptMap()
        {
            //
            // 打乱拼图板（需要合法的拼图板）。
            //

            if (GetZeroCountOfArray(ElementArray, Range) == 1)
            {
                double N = 0.368299958486299 * Math.Pow(Range.Width + Range.Height, 2.69505569810127); // 此多项式基于大量统计数据的 Excel 拟合，其含义为打乱一个布局的宽度与高度之和为特定常数的拼图板所需步数的数学期望。
                int n = 0;

                while (CorrectTileCount > 0 || n < 2 * N) // 用于确保打乱后的拼图板具有较高的混乱程度。
                {
                    Point Az = GetCertainIndexListOfArray(ElementArray, Range, 0)[0];

                    List<Point> L_Ax = new List<Point>(0);

                    for (int X = 0; X < Range.Width; X++)
                    {
                        if (X != Az.X)
                        {
                            L_Ax.Add(new Point(X, Az.Y));
                        }
                    }

                    for (int Y = 0; Y < Range.Height; Y++)
                    {
                        if (Y != Az.Y)
                        {
                            L_Ax.Add(new Point(Az.X, Y));
                        }
                    }

                    _ElementArray_LogicalMove(L_Ax[Com.Statistics.RandomInteger(L_Ax.Count)], Az);

                    n++;
                }
            }
        }

        #endregion

        #region 背景图片

        // 背景图片。

        private static readonly Size BkgImgMinSize = new Size(300, 300); // 背景图片最小尺寸。

        private Bitmap BkgImg; // 背景图片的原始副本位图。

        private bool CheckBkgImg()
        {
            //
            // 检查背景图片的合法性，并返回背景图片是否合法。
            //

            try
            {
                if (BkgImg != null && (BkgImg.Width >= BkgImgMinSize.Width && BkgImg.Height >= BkgImgMinSize.Height))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool LoadBkgImg()
        {
            //
            // 从图像资源文件加载背景图片，更新子图像数组，并返回是否加载成功。
            //

            try
            {
                if (File.Exists(ImgresFilePath))
                {
                    if (new FileInfo(ImgresFilePath).Length > 0)
                    {
                        try
                        {
                            Image Img = Image.FromFile(ImgresFilePath);

                            if (BkgImg != null)
                            {
                                BkgImg.Dispose();
                            }

                            BkgImg = new Bitmap(Img);

                            Img.Dispose();

                            if (CheckBkgImg())
                            {
                                Size ThumbSize = Panel_BkgImg.Size;

                                if (BkgImg.Width * Panel_BkgImg.Height > Panel_BkgImg.Width * BkgImg.Height)
                                {
                                    ThumbSize.Width = Panel_BkgImg.Width;
                                    ThumbSize.Height = Panel_BkgImg.Width * BkgImg.Height / BkgImg.Width;
                                }
                                else if (BkgImg.Width * Panel_BkgImg.Height < Panel_BkgImg.Width * BkgImg.Height)
                                {
                                    ThumbSize.Width = Panel_BkgImg.Height * BkgImg.Width / BkgImg.Height;
                                    ThumbSize.Height = Panel_BkgImg.Height;
                                }

                                Panel_BkgImg.BackgroundImage = new Bitmap(BkgImg, ThumbSize);

                                //

                                ResetSubImgAry();

                                //

                                return true;
                            }

                            return false;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool SelectBkgImg(string FilePath)
        {
            //
            // 选择外部文件并应用为背景图片，并返回是否应用成功。
            //

            try
            {
                if (File.Exists(FilePath))
                {
                    if (new FileInfo(FilePath).Length > 0)
                    {
                        try
                        {
                            Image Img = Image.FromFile(FilePath);

                            Bitmap Bmp = new Bitmap(Img);

                            Img.Dispose();

                            if (Bmp != null && (Bmp.Width >= BkgImgMinSize.Width && Bmp.Height >= BkgImgMinSize.Height))
                            {
                                if (!Directory.Exists(ResFileDir))
                                {
                                    Directory.CreateDirectory(ResFileDir);
                                }

                                if (File.Exists(ImgresFilePath))
                                {
                                    new FileInfo(ImgresFilePath).Attributes = FileAttributes.Normal;
                                }

                                File.Copy(FilePath, ImgresFilePath, true);

                                if (File.Exists(ImgresFilePath))
                                {
                                    new FileInfo(ImgresFilePath).Attributes = FileAttributes.Normal;
                                }

                                return LoadBkgImg();
                            }

                            return false;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // 子图像数组。

        private Bitmap[,] SubImgAry = new Bitmap[CAPACITY, CAPACITY]; // 子图像数组（背景图片的原始副本位图按当前布局裁切分块后的位图数组）。
        private Bitmap[,] SubImgAry_Gray = new Bitmap[CAPACITY, CAPACITY]; // 子图像数组（灰度）（背景图片的原始副本位图按当前布局裁切分块后的位图数组）。

        private Size SubImgAry_EffCap = new Size(); // 子图像数组当前在宽度和高度方向上实际包含的位图数量。

        private bool ResetSubImgAry()
        {
            //
            // 重置子图像数组。
            //

            if (BkgImg != null)
            {
                SubImgAry_EffCap = Range;

                Int32 SubImgSize = Math.Min(BkgImg.Width / Range.Width, BkgImg.Height / Range.Height);
                Point SubImgOffset = new Point((BkgImg.Width - SubImgSize * Range.Width) / 2, (BkgImg.Height - SubImgSize * Range.Height) / 2);

                for (int X = 0; X < Range.Width; X++)
                {
                    for (int Y = 0; Y < Range.Height; Y++)
                    {
                        Bitmap SubImg = new Bitmap(SubImgSize, SubImgSize);
                        Graphics SubImgGrap = Graphics.FromImage(SubImg);

                        SubImgGrap.DrawImage(BkgImg, 0, 0, new Rectangle(SubImgOffset.X + X * SubImgSize, SubImgOffset.Y + Y * SubImgSize, SubImgSize, SubImgSize), GraphicsUnit.Pixel);

                        SubImgAry[X, Y] = (Bitmap)SubImg.Clone();

                        SubImg.Dispose();
                        SubImgGrap.Dispose();

                        //

                        ColorMatrix CrMtrx = new ColorMatrix();

                        for (int i = 0; i < 3; i++)
                        {
                            CrMtrx[0, i] = 0.2126F;
                            CrMtrx[1, i] = 0.7152F;
                            CrMtrx[2, i] = 0.0722F;
                        }

                        ImageAttributes ImgAttr = new ImageAttributes();
                        ImgAttr.SetColorMatrix(CrMtrx, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                        Bitmap SubImg_Gray = new Bitmap(SubImgSize, SubImgSize);
                        Graphics SubImgGrap_Gray = Graphics.FromImage(SubImg_Gray);

                        SubImgGrap_Gray.DrawImage(BkgImg, new Rectangle(0, 0, SubImgSize, SubImgSize), SubImgOffset.X + X * SubImgSize, SubImgOffset.Y + Y * SubImgSize, SubImgSize, SubImgSize, GraphicsUnit.Pixel, ImgAttr);

                        SubImgAry_Gray[X, Y] = (Bitmap)SubImg_Gray.Clone();

                        SubImg_Gray.Dispose();
                        SubImgGrap_Gray.Dispose();
                    }
                }

                return true;
            }

            return false;
        }

        #endregion

        #region 计时器

        private CycData CD_Timer = new CycData(); // Timer_Timer 计时周期数据。

        private void Timer_Timer_Tick(object sender, EventArgs e)
        {
            //
            // Timer_Timer。
            //

            TimerWorkOnce();
        }

        private void TimerStart()
        {
            //
            // 计时器开始。
            //

            CD_Timer.Reset();

            //

            TimerUpdateInterval();

            //

            Timer_Timer.Enabled = true;
        }

        private void TimerWorkOnce()
        {
            //
            // 计时器进行一次工作。
            //

            CD_Timer.Update();

            //

            ThisRecord.GameTime += TimeSpan.FromMilliseconds(CD_Timer.DeltaMS);

            ThisGameTime += TimeSpan.FromMilliseconds(CD_Timer.DeltaMS);
            TotalGameTime += TimeSpan.FromMilliseconds(CD_Timer.DeltaMS);

            RepaintCurBmp();

            //

            TimerUpdateInterval();
        }

        private void TimerStop()
        {
            //
            // 计时器停止。
            //

            Timer_Timer.Enabled = false;
        }

        private void TimerUpdateInterval()
        {
            //
            // 计时器更新工作周期。
            //

            if (ThisRecord.GameTime.TotalSeconds < 1)
            {
                Timer_Timer.Interval = 10;
            }
            else if (ThisRecord.GameTime.TotalSeconds < 10)
            {
                Timer_Timer.Interval = 50;
            }
            else if (ThisRecord.GameTime.TotalSeconds < 60)
            {
                Timer_Timer.Interval = 100;
            }
            else
            {
                Timer_Timer.Interval = 200;
            }
        }

        #endregion

        #region 中断管理

        // 判定。

        private void Judgement()
        {
            //
            // 完成判定。
            //

            if (!GameIsWin)
            {
                if (CorrectTileCount >= TotalTileCount)
                {
                    GameIsWin = true;

                    TimerStop();

                    ElementArray_AnimatePresentAt(GetCorrectIndexFromValue(0));

                    ElementArray_RepresentAll();

                    ThisRecord.Range = Range;

                    SaveUserData();

                    EraseLastGame();

                    PictureBox_Interrupt.Enabled = false;
                }
            }

            //

            RepaintCurBmp();
        }

        // 中断。

        private enum InterruptActions { NULL = -1, StartNew, Continue, Pause, Resume, Restart, Exit, CloseApp, COUNT } // 中断动作枚举。

        private void Interrupt(InterruptActions IA)
        {
            //
            // 中断。
            //

            switch (IA)
            {
                case InterruptActions.StartNew: // 开始新游戏。
                    {
                        EraseLastGame();

                        //

                        if (BlockStyle == BlockStyles.Image && SubImgAry_EffCap != Range)
                        {
                            ResetSubImgAry();
                        }

                        //

                        EnterGameUI();

                        //

                        CreateMap();
                        DisruptMap();

                        TimerStart();

                        ElementArray_AnimatePresentAt(ElementIndexList, true);

                        Judgement();
                    }
                    break;

                case InterruptActions.Continue: // 继续上次的游戏。
                    {
                        Range = Record_Last.Range;

                        //

                        if (BlockStyle == BlockStyles.Image && SubImgAry_EffCap != Range)
                        {
                            ResetSubImgAry();
                        }

                        //

                        EnterGameUI();

                        //

                        ComboBox_Range_Width.SelectedIndexChanged -= ComboBox_Range_Width_SelectedIndexChanged;
                        ComboBox_Range_Height.SelectedIndexChanged -= ComboBox_Range_Height_SelectedIndexChanged;

                        ComboBox_Range_Width.SelectedIndex = ComboBox_Range_Width.Items.IndexOf(Range.Width.ToString());
                        ComboBox_Range_Height.SelectedIndex = ComboBox_Range_Height.Items.IndexOf(Range.Height.ToString());

                        ComboBox_Range_Width.SelectedIndexChanged += ComboBox_Range_Width_SelectedIndexChanged;
                        ComboBox_Range_Height.SelectedIndexChanged += ComboBox_Range_Height_SelectedIndexChanged;

                        //

                        ElementArray_Initialize();

                        foreach (var V in ElementIndexList_Last)
                        {
                            ElementArray_Add(V, ElementArray_Last[V.X, V.Y]);
                        }

                        ThisRecord.GameTime = Record_Last.GameTime;
                        ThisRecord.StepCount = Record_Last.StepCount;

                        TimerStart();

                        ElementArray_AnimatePresentAt(ElementIndexList, true);

                        Judgement();
                    }
                    break;

                case InterruptActions.Pause: // 暂停。
                    {
                        TimerStop();

                        ElementArray_RepresentAll();

                        RepaintCurBmp();

                        PictureBox_Interrupt.Image = Properties.Resources.Resume;
                    }
                    break;

                case InterruptActions.Resume: // 恢复。
                    {
                        TimerStart();

                        ElementArray_RepresentAll();

                        Judgement();

                        PictureBox_Interrupt.Image = Properties.Resources.Pause;
                    }
                    break;

                case InterruptActions.Restart: // 重新开始。
                    {
                        EraseLastGame();

                        //

                        ThisRecord.Range = Range;

                        SaveUserData();

                        //

                        ThisGameTime = TimeSpan.Zero;

                        GameIsWin = false;

                        ThisRecord = new Record();

                        ElementArray_Initialize();

                        CreateMap();
                        DisruptMap();

                        RepaintCurBmp();

                        ElementArray_AnimatePresentAt(ElementIndexList, true);

                        TimerStart();

                        ElementArray_RepresentAll();

                        PictureBox_Interrupt.Enabled = true;
                        PictureBox_Interrupt.Image = Properties.Resources.Pause;

                        Judgement();

                        //

                        Panel_Environment.Focus();
                    }
                    break;

                case InterruptActions.Exit: // 退出游戏。
                    {
                        ThisRecord.Range = Range;

                        SaveUserData();

                        //

                        Panel_Environment.Focus();

                        //

                        if (!GameIsWin)
                        {
                            if (ThisRecord.GameTime.TotalMilliseconds > 0 && ThisRecord.StepCount > 0)
                            {
                                SaveLastGame();
                            }

                            ThisRecord.GameTime = TimeSpan.Zero;
                            ThisRecord.StepCount = 0;
                        }

                        ExitGameUI();
                    }
                    break;

                case InterruptActions.CloseApp: // 关闭程序。
                    {
                        ThisRecord.Range = Range;

                        SaveUserData();

                        //

                        Panel_Environment.Focus();

                        //

                        if (!GameIsWin && (ThisRecord.GameTime.TotalMilliseconds > 0 && ThisRecord.StepCount > 0))
                        {
                            SaveLastGame();
                        }

                        ExitGameUI();
                    }
                    break;
            }
        }

        // 中断按钮。

        private void Label_StartNewGame_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_StartNewGame。
            //

            Interrupt(InterruptActions.StartNew);
        }

        private void Label_ContinueLastGame_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_ContinueLastGame。
            //

            Interrupt(InterruptActions.Continue);
        }

        private void PictureBox_Interrupt_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 PictureBox_Interrupt。
            //

            ToolTip_InterruptPrompt.RemoveAll();

            if (!GameIsWin)
            {
                if (Timer_Timer.Enabled)
                {
                    ToolTip_InterruptPrompt.SetToolTip(PictureBox_Interrupt, "暂停");
                }
                else
                {
                    ToolTip_InterruptPrompt.SetToolTip(PictureBox_Interrupt, "恢复");
                }
            }
        }

        private void PictureBox_Interrupt_Click(object sender, EventArgs e)
        {
            //
            // 单击 PictureBox_Interrupt。
            //

            if (!GameIsWin)
            {
                if (Timer_Timer.Enabled)
                {
                    Interrupt(InterruptActions.Pause);
                }
                else
                {
                    Interrupt(InterruptActions.Resume);
                }
            }
        }

        private void PictureBox_Restart_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 PictureBox_Restart。
            //

            ToolTip_InterruptPrompt.RemoveAll();

            ToolTip_InterruptPrompt.SetToolTip(PictureBox_Restart, "重新开始");
        }

        private void PictureBox_Restart_Click(object sender, EventArgs e)
        {
            //
            // 单击 PictureBox_Restart。
            //

            Interrupt(InterruptActions.Restart);
        }

        private void PictureBox_ExitGame_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 PictureBox_ExitGame。
            //

            ToolTip_InterruptPrompt.RemoveAll();

            ToolTip_InterruptPrompt.SetToolTip(PictureBox_ExitGame, (!GameIsWin && ThisRecord.StepCount > 0 ? "保存并退出" : "退出"));
        }

        private void PictureBox_ExitGame_Click(object sender, EventArgs e)
        {
            //
            // 单击 PictureBox_ExitGame。
            //

            Interrupt(InterruptActions.Exit);
        }

        #endregion

        #region UI 切换

        private bool GameUINow = false; // 当前 UI 是否为游戏 UI。

        private void EnterGameUI()
        {
            //
            // 进入游戏 UI。
            //

            GameUINow = true;

            //

            ElementArray_Initialize();

            //

            GameIsWin = false;

            ThisGameTime = TimeSpan.Zero;

            ThisRecord = new Record();

            PictureBox_Interrupt.Enabled = true;
            PictureBox_Interrupt.Image = Properties.Resources.Pause;

            //

            Panel_FunctionArea.Visible = false;
            Panel_GameUI.Visible = true;

            //

            Panel_Environment.Focus();

            //

            while (ElementSize * Range.Width > Screen.PrimaryScreen.WorkingArea.Width || Me.CaptionBarHeight + Panel_Current.Height + ElementSize * Range.Height > Screen.PrimaryScreen.WorkingArea.Height)
            {
                ElementSize = ElementSize * 9 / 10;
            }

            Me.ClientSize = new Size(ElementSize * Range.Width, Panel_Current.Height + ElementSize * Range.Height);
            Me.Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - Me.Width) / 2, (Screen.PrimaryScreen.WorkingArea.Height - Me.Height) / 2);

            ElementSize = Math.Max(1, Math.Min(Panel_Environment.Width / Range.Width, Panel_Environment.Height / Range.Height));

            EAryBmpRect.Size = new Size(Math.Max(1, ElementSize * Range.Width), Math.Max(1, ElementSize * Range.Height));
            EAryBmpRect.Location = new Point((Panel_Environment.Width - EAryBmpRect.Width) / 2, (Panel_Environment.Height - EAryBmpRect.Height) / 2);

            //

            RepaintCurBmp();

            ElementArray_RepresentAll();
        }

        private void ExitGameUI()
        {
            //
            // 退出游戏 UI。
            //

            GameUINow = false;

            //

            TimerStop();

            Panel_FunctionArea.Visible = true;
            Panel_GameUI.Visible = false;

            //

            ElementArray_Initialize();

            //

            Me.ClientSize = FormClientInitialSize;
            Me.Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - Me.Width) / 2, (Screen.PrimaryScreen.WorkingArea.Height - Me.Height) / 2);

            //

            FunctionAreaTab = FunctionAreaTabs.Start;
        }

        #endregion

        #region 游戏 UI 交互

        private void Panel_Environment_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Panel_Environment。
            //

            Panel_Environment.Focus();
        }

        private void Panel_Environment_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Panel_Environment。
            //

            if (Timer_Timer.Enabled && e.Button == MouseButtons.Left)
            {
                Point A = ElementArray_GetIndex(Com.Geometry.GetCursorPositionOfControl(Panel_Environment));

                if (A.X >= 0 && A.X < Range.Width && A.Y >= 0 && A.Y < Range.Height)
                {
                    if (ElementArray[A.X, A.Y] != 0)
                    {
                        TimerWorkOnce();

                        ElementArray_LogicalMove(A);
                    }
                }
            }
        }

        private void Panel_Environment_KeyDown(object sender, KeyEventArgs e)
        {
            //
            // 在 Panel_Environment 按下键。
            //

            if (!GameIsWin)
            {
                if (e.KeyCode == Keys.Space)
                {
                    if (Timer_Timer.Enabled)
                    {
                        Interrupt(InterruptActions.Pause);
                    }
                    else
                    {
                        Interrupt(InterruptActions.Resume);
                    }
                }
            }

            switch (e.KeyCode)
            {
                case Keys.Home: Interrupt(InterruptActions.Restart); break;
                case Keys.End:
                case Keys.Escape: Interrupt(InterruptActions.Exit); break;
            }
        }

        #endregion

        #region 鼠标滚轮功能

        private void Form_Main_MouseWheel(object sender, MouseEventArgs e)
        {
            //
            // 鼠标滚轮在 this 滚动。
            //

            if (Panel_FunctionAreaOptionsBar.Visible && Panel_FunctionAreaOptionsBar.Enabled && Com.Geometry.CursorIsInControl(Panel_FunctionAreaOptionsBar))
            {
                if (e.Delta < 0 && (Int32)FunctionAreaTab < (Int32)FunctionAreaTabs.COUNT - 1)
                {
                    FunctionAreaTab++;
                }
                else if (e.Delta > 0 && (Int32)FunctionAreaTab > 0)
                {
                    FunctionAreaTab--;
                }
            }
        }

        private void Panel_Environment_MouseWheel(object sender, MouseEventArgs e)
        {
            //
            // 鼠标滚轮在 Panel_Environment 滚动。
            //

            if (Range.Width <= Range.Height)
            {
                if (e.Delta > 0)
                {
                    Me.Location = new Point(Me.X - Me.Width / 20, Me.Y - Me.Width / 20 * Range.Height / Range.Width);
                    Me.Size = new Size(Me.Width + Me.Width / 20 * 2, Me.Height + Me.Width / 20 * Range.Height / Range.Width * 2);
                }
                else if (e.Delta < 0)
                {
                    Me.Location = new Point(Me.X + Me.Width / 20, Me.Y + Me.Width / 20 * Range.Height / Range.Width);
                    Me.Size = new Size(Me.Width - Me.Width / 20 * 2, Me.Height - Me.Width / 20 * Range.Height / Range.Width * 2);
                }
            }
            else
            {
                if (e.Delta > 0)
                {
                    Me.Location = new Point(Me.X - Me.Height / 20 * Range.Width / Range.Height, Me.Y - Me.Height / 20);
                    Me.Size = new Size(Me.Width + Me.Height / 20 * Range.Width / Range.Height * 2, Me.Height + Me.Height / 20 * 2);
                }
                else if (e.Delta < 0)
                {
                    Me.Location = new Point(Me.X + Me.Height / 20 * Range.Width / Range.Height, Me.Y + Me.Height / 20);
                    Me.Size = new Size(Me.Width - Me.Height / 20 * Range.Width / Range.Height * 2, Me.Height - Me.Height / 20 * 2);
                }
            }

            Me.Location = new Point(Math.Max(0, Math.Min(Screen.PrimaryScreen.WorkingArea.Width - Me.Width, Me.X)), Math.Max(0, Math.Min(Screen.PrimaryScreen.WorkingArea.Height - Me.Height, Me.Y)));
        }

        #endregion

        #region 计分栏

        private Bitmap CurBmp; // 计分栏位图。

        private Graphics CurBmpGrap; // 计分栏位图绘图。

        private void UpdateCurBmp()
        {
            //
            // 更新计分栏位图。
            //

            if (CurBmp != null)
            {
                CurBmp.Dispose();
            }

            CurBmp = new Bitmap(Math.Max(1, Panel_Current.Width), Math.Max(1, Panel_Current.Height));

            CurBmpGrap = Graphics.FromImage(CurBmp);

            if (AntiAlias)
            {
                CurBmpGrap.SmoothingMode = SmoothingMode.AntiAlias;
                CurBmpGrap.TextRenderingHint = TextRenderingHint.AntiAlias;
            }

            CurBmpGrap.Clear(GameUIBackColor_DEC);

            //

            Rectangle Rect_Total = new Rectangle(new Point(0, 0), new Size(Math.Max(1, Panel_Current.Width), Math.Max(1, Panel_Current.Height)));
            Rectangle Rect_Current = new Rectangle(Rect_Total.Location, new Size((Int32)Math.Max(2, Math.Min(1, (double)CorrectTileCount / TotalTileCount) * Rect_Total.Width), Rect_Total.Height));

            Color RectCr_Total = Me.RecommendColors.Background.ToColor(), RectCr_Current = Me.RecommendColors.Border.ToColor();

            GraphicsPath Path_Total = new GraphicsPath();
            Path_Total.AddRectangle(Rect_Total);
            PathGradientBrush PGB_Total = new PathGradientBrush(Path_Total)
            {
                CenterColor = RectCr_Total,
                SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(RectCr_Total, 0.3) },
                FocusScales = new PointF(1F, 0F)
            };
            CurBmpGrap.FillPath(PGB_Total, Path_Total);
            Path_Total.Dispose();
            PGB_Total.Dispose();

            GraphicsPath Path_Current = new GraphicsPath();
            Path_Current.AddRectangle(Rect_Current);
            PathGradientBrush PGB_Current = new PathGradientBrush(Path_Current)
            {
                CenterColor = RectCr_Current,
                SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(RectCr_Current, 0.3) },
                FocusScales = new PointF(1F, 0F)
            };
            CurBmpGrap.FillPath(PGB_Current, Path_Current);
            Path_Current.Dispose();
            PGB_Current.Dispose();

            //

            SizeF RegionSize_L = new SizeF(), RegionSize_R = new SizeF();
            RectangleF RegionRect = new RectangleF();

            string StringText_Score = Com.Text.GetTimeStringFromTimeSpan(ThisRecord.GameTime);
            Color StringColor_Score = Me.RecommendColors.Text_INC.ToColor();
            Font StringFont_Score = new Font("微软雅黑", 24F, FontStyle.Regular, GraphicsUnit.Point, 134);
            RectangleF StringRect_Score = new RectangleF();
            StringRect_Score.Size = CurBmpGrap.MeasureString(StringText_Score, StringFont_Score);

            string StringText_Complete = "已归位: ", StringText_Complete_Val = Math.Max(0, CorrectTileCount) + " / " + Math.Max(0, TotalTileCount);
            Color StringColor_Complete = Me.RecommendColors.Text.ToColor(), StringColor_Complete_Val = Me.RecommendColors.Text_INC.ToColor();
            Font StringFont_Complete = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134), StringFont_Complete_Val = new Font("微软雅黑", 12F, FontStyle.Bold, GraphicsUnit.Point, 134);
            RectangleF StringRect_Complete = new RectangleF(), StringRect_Complete_Val = new RectangleF();
            StringRect_Complete.Size = CurBmpGrap.MeasureString(StringText_Complete, StringFont_Complete);
            StringRect_Complete_Val.Size = CurBmpGrap.MeasureString(StringText_Complete_Val, StringFont_Complete_Val);

            string StringText_StepCount = "步数: ", StringText_StepCount_Val = Math.Max(0, ThisRecord.StepCount).ToString();
            Color StringColor_StepCount = Me.RecommendColors.Text.ToColor(), StringColor_StepCount_Val = Me.RecommendColors.Text_INC.ToColor();
            Font StringFont_StepCount = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134), StringFont_StepCount_Val = new Font("微软雅黑", 12F, FontStyle.Bold, GraphicsUnit.Point, 134);
            RectangleF StringRect_StepCount = new RectangleF(), StringRect_StepCount_Val = new RectangleF();
            StringRect_StepCount.Size = CurBmpGrap.MeasureString(StringText_StepCount, StringFont_StepCount);
            StringRect_StepCount_Val.Size = CurBmpGrap.MeasureString(StringText_StepCount_Val, StringFont_StepCount_Val);

            RegionSize_L = StringRect_Score.Size;
            RegionSize_R = new SizeF(Math.Max(StringRect_Complete.Width + StringRect_Complete_Val.Width, StringRect_StepCount.Width + StringRect_StepCount_Val.Width), 0);

            RegionRect.Size = new SizeF(Math.Max(RegionSize_L.Width + RegionSize_R.Width, Math.Min(EAryBmpRect.Width, Panel_Interrupt.Left - EAryBmpRect.X)), Panel_Current.Height);
            RegionRect.Location = new PointF(Math.Max(0, Math.Min(EAryBmpRect.X + (EAryBmpRect.Width - RegionRect.Width) / 2, Panel_Interrupt.Left - RegionRect.Width)), 0);

            StringRect_Score.Location = new PointF(RegionRect.X, (RegionRect.Height - StringRect_Score.Height) / 2);

            Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_Score, StringFont_Score, StringColor_Score, StringColor_Score, StringRect_Score.Location, 0.05F, AntiAlias);

            StringRect_Complete_Val.Location = new PointF(RegionRect.Right - StringRect_Complete_Val.Width, (RegionRect.Height / 2 - StringRect_Complete_Val.Height) / 2);
            StringRect_Complete.Location = new PointF(StringRect_Complete_Val.X - StringRect_Complete.Width, (RegionRect.Height / 2 - StringRect_Complete.Height) / 2);

            Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_Complete, StringFont_Complete, StringColor_Complete, StringColor_Complete, StringRect_Complete.Location, 0.1F, AntiAlias);
            Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_Complete_Val, StringFont_Complete_Val, StringColor_Complete_Val, StringColor_Complete_Val, StringRect_Complete_Val.Location, 0.1F, AntiAlias);

            StringRect_StepCount_Val.Location = new PointF(RegionRect.Right - StringRect_StepCount_Val.Width, RegionRect.Height / 2 + (RegionRect.Height / 2 - StringRect_StepCount_Val.Height) / 2);
            StringRect_StepCount.Location = new PointF(StringRect_StepCount_Val.X - StringRect_StepCount.Width, RegionRect.Height / 2 + (RegionRect.Height / 2 - StringRect_StepCount.Height) / 2);

            Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_StepCount, StringFont_StepCount, StringColor_StepCount, StringColor_StepCount, StringRect_StepCount.Location, 0.1F, AntiAlias);
            Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_StepCount_Val, StringFont_StepCount_Val, StringColor_StepCount_Val, StringColor_StepCount_Val, StringRect_StepCount_Val.Location, 0.1F, AntiAlias);
        }

        private void RepaintCurBmp()
        {
            //
            // 更新并重绘计分栏位图。
            //

            UpdateCurBmp();

            if (CurBmp != null)
            {
                Panel_Current.CreateGraphics().DrawImage(CurBmp, new Point(0, 0));

                foreach (var V in Panel_Current.Controls)
                {
                    ((Control)V).Refresh();
                }
            }
        }

        private void Panel_Current_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_Current 绘图。
            //

            UpdateCurBmp();

            if (CurBmp != null)
            {
                e.Graphics.DrawImage(CurBmp, new Point(0, 0));
            }
        }

        #endregion

        #region 功能区

        private enum FunctionAreaTabs { NULL = -1, Start, Record, Options, About, COUNT } // 功能区选项卡枚举。

        private FunctionAreaTabs _FunctionAreaTab = FunctionAreaTabs.NULL; // 当前打开的功能区选项卡。
        private FunctionAreaTabs FunctionAreaTab
        {
            get
            {
                return _FunctionAreaTab;
            }

            set
            {
                _FunctionAreaTab = value;

                Color TabBtnCr_Fr_Seld = Me.RecommendColors.Main_INC.ToColor(), TabBtnCr_Fr_Uns = Color.White;
                Color TabBtnCr_Bk_Seld = Color.Transparent, TabBtnCr_Bk_Uns = Color.Transparent;
                Font TabBtnFt_Seld = new Font("微软雅黑", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 134), TabBtnFt_Uns = new Font("微软雅黑", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 134);

                Label_Tab_Start.ForeColor = (_FunctionAreaTab == FunctionAreaTabs.Start ? TabBtnCr_Fr_Seld : TabBtnCr_Fr_Uns);
                Label_Tab_Start.BackColor = (_FunctionAreaTab == FunctionAreaTabs.Start ? TabBtnCr_Bk_Seld : TabBtnCr_Bk_Uns);
                Label_Tab_Start.Font = (_FunctionAreaTab == FunctionAreaTabs.Start ? TabBtnFt_Seld : TabBtnFt_Uns);

                Label_Tab_Record.ForeColor = (_FunctionAreaTab == FunctionAreaTabs.Record ? TabBtnCr_Fr_Seld : TabBtnCr_Fr_Uns);
                Label_Tab_Record.BackColor = (_FunctionAreaTab == FunctionAreaTabs.Record ? TabBtnCr_Bk_Seld : TabBtnCr_Bk_Uns);
                Label_Tab_Record.Font = (_FunctionAreaTab == FunctionAreaTabs.Record ? TabBtnFt_Seld : TabBtnFt_Uns);

                Label_Tab_Options.ForeColor = (_FunctionAreaTab == FunctionAreaTabs.Options ? TabBtnCr_Fr_Seld : TabBtnCr_Fr_Uns);
                Label_Tab_Options.BackColor = (_FunctionAreaTab == FunctionAreaTabs.Options ? TabBtnCr_Bk_Seld : TabBtnCr_Bk_Uns);
                Label_Tab_Options.Font = (_FunctionAreaTab == FunctionAreaTabs.Options ? TabBtnFt_Seld : TabBtnFt_Uns);

                Label_Tab_About.ForeColor = (_FunctionAreaTab == FunctionAreaTabs.About ? TabBtnCr_Fr_Seld : TabBtnCr_Fr_Uns);
                Label_Tab_About.BackColor = (_FunctionAreaTab == FunctionAreaTabs.About ? TabBtnCr_Bk_Seld : TabBtnCr_Bk_Uns);
                Label_Tab_About.Font = (_FunctionAreaTab == FunctionAreaTabs.About ? TabBtnFt_Seld : TabBtnFt_Uns);

                switch (_FunctionAreaTab)
                {
                    case FunctionAreaTabs.Start:
                        {
                            if (ElementIndexList_Last.Count > 0)
                            {
                                Label_ContinueLastGame.Visible = true;

                                Label_ContinueLastGame.Focus();
                            }
                            else
                            {
                                Label_ContinueLastGame.Visible = false;

                                Label_StartNewGame.Focus();
                            }
                        }
                        break;

                    case FunctionAreaTabs.Record:
                        {
                            if (BestRecord.GameTime.TotalMilliseconds == 0 || BestRecord.StepCount == 0)
                            {
                                Label_ThisRecordVal_GameTime.Text = "无记录";
                                Label_ThisRecordVal_StepCount.Text = "步数: 无";
                                Label_BestRecordVal_GameTime.Text = "无记录";
                                Label_BestRecordVal_StepCount.Text = "步数: 无";
                            }
                            else
                            {
                                Record ThRec = new Record();

                                if (ThisRecord.Range == Range)
                                {
                                    ThRec = ThisRecord;
                                }

                                Label_ThisRecordVal_GameTime.Text = Com.Text.GetTimeStringFromTimeSpan(ThRec.GameTime);
                                Label_ThisRecordVal_StepCount.Text = "步数: " + ThRec.StepCount;
                                Label_BestRecordVal_GameTime.Text = Com.Text.GetTimeStringFromTimeSpan(BestRecord.GameTime);
                                Label_BestRecordVal_StepCount.Text = "步数: " + BestRecord.StepCount;
                            }

                            Label_ThisTimeVal.Text = Com.Text.GetTimeStringFromTimeSpan(ThisGameTime);
                            Label_TotalTimeVal.Text = Com.Text.GetTimeStringFromTimeSpan(TotalGameTime);
                        }
                        break;

                    case FunctionAreaTabs.Options:
                        {
                            if (!CheckBkgImg())
                            {
                                BlockStyle = BlockStyles.Number;
                            }

                            ResetBlockStyleControl();
                        }
                        break;

                    case FunctionAreaTabs.About:
                        {

                        }
                        break;
                }

                Timer_EnterPrompt.Enabled = (_FunctionAreaTab == FunctionAreaTabs.Start);

                if (Panel_FunctionAreaTab.AutoScroll)
                {
                    // Panel 的 AutoScroll 功能似乎存在 bug，下面的代码可以规避某些显示问题

                    Panel_FunctionAreaTab.AutoScroll = false;

                    foreach (var V in Panel_FunctionAreaTab.Controls)
                    {
                        if (V is Panel)
                        {
                            Panel Pnl = V as Panel;

                            Pnl.Location = new Point(0, 0);
                        }
                    }

                    Panel_FunctionAreaTab.AutoScroll = true;
                }

                Panel_Tab_Start.Visible = (_FunctionAreaTab == FunctionAreaTabs.Start);
                Panel_Tab_Record.Visible = (_FunctionAreaTab == FunctionAreaTabs.Record);
                Panel_Tab_Options.Visible = (_FunctionAreaTab == FunctionAreaTabs.Options);
                Panel_Tab_About.Visible = (_FunctionAreaTab == FunctionAreaTabs.About);
            }
        }

        private void Label_Tab_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 Label_Tab。
            //

            Panel_FunctionAreaOptionsBar.Refresh();
        }

        private void Label_Tab_MouseLeave(object sender, EventArgs e)
        {
            //
            // 鼠标离开 Label_Tab。
            //

            Panel_FunctionAreaOptionsBar.Refresh();
        }

        private void Label_Tab_Start_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Tab_Start。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (FunctionAreaTab != FunctionAreaTabs.Start)
                {
                    FunctionAreaTab = FunctionAreaTabs.Start;
                }
            }
        }

        private void Label_Tab_Record_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Tab_Record。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (FunctionAreaTab != FunctionAreaTabs.Record)
                {
                    FunctionAreaTab = FunctionAreaTabs.Record;
                }
            }
        }

        private void Label_Tab_Options_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Tab_Options。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (FunctionAreaTab != FunctionAreaTabs.Options)
                {
                    FunctionAreaTab = FunctionAreaTabs.Options;
                }
            }
        }

        private void Label_Tab_About_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Tab_About。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (FunctionAreaTab != FunctionAreaTabs.About)
                {
                    FunctionAreaTab = FunctionAreaTabs.About;
                }
            }
        }

        #endregion

        #region "开始"区域

        private const Int32 EnterGameButtonHeight_Min = 30, EnterGameButtonHeight_Max = 50; // 进入游戏按钮高度的取值范围。

        private Color EnterGameBackColor_INC = Color.Empty; // Panel_EnterGameSelection 绘图使用的颜色（深色）。
        private Color EnterGameBackColor_DEC => Panel_FunctionArea.BackColor; // Panel_EnterGameSelection 绘图使用的颜色（浅色）。

        private void Panel_EnterGameSelection_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_EnterGameSelection 绘图。
            //

            Rectangle Rect_StartNew = new Rectangle(Label_StartNewGame.Location, Label_StartNewGame.Size);

            Color Cr_StartNew = Com.ColorManipulation.BlendByRGB(EnterGameBackColor_INC, EnterGameBackColor_DEC, Math.Sqrt((double)(Label_StartNewGame.Height - EnterGameButtonHeight_Min) / (EnterGameButtonHeight_Max - EnterGameButtonHeight_Min)));

            GraphicsPath Path_StartNew = new GraphicsPath();
            Path_StartNew.AddRectangle(Rect_StartNew);
            PathGradientBrush PGB_StartNew = new PathGradientBrush(Path_StartNew)
            {
                CenterColor = Cr_StartNew,
                SurroundColors = new Color[] { Com.ColorManipulation.BlendByRGB(Cr_StartNew, EnterGameBackColor_DEC, 0.7) },
                FocusScales = new PointF(1F, 0F)
            };
            e.Graphics.FillPath(PGB_StartNew, Path_StartNew);
            Path_StartNew.Dispose();
            PGB_StartNew.Dispose();

            //

            if (Label_ContinueLastGame.Visible)
            {
                Rectangle Rect_Continue = new Rectangle(Label_ContinueLastGame.Location, Label_ContinueLastGame.Size);

                Color Cr_Continue = Com.ColorManipulation.BlendByRGB(EnterGameBackColor_INC, EnterGameBackColor_DEC, Math.Sqrt((double)(Label_ContinueLastGame.Height - EnterGameButtonHeight_Min) / (EnterGameButtonHeight_Max - EnterGameButtonHeight_Min)));

                GraphicsPath Path_Continue = new GraphicsPath();
                Path_Continue.AddRectangle(Rect_Continue);
                PathGradientBrush PGB_Continue = new PathGradientBrush(Path_Continue)
                {
                    CenterColor = Cr_Continue,
                    SurroundColors = new Color[] { Com.ColorManipulation.BlendByRGB(Cr_Continue, EnterGameBackColor_DEC, 0.7) },
                    FocusScales = new PointF(1F, 0F)
                };
                e.Graphics.FillPath(PGB_Continue, Path_Continue);
                Path_Continue.Dispose();
                PGB_Continue.Dispose();
            }
        }

        private double EnterPrompt_Val = 0; // 闪烁相位。
        private double EnterPrompt_Step = 0.025; // 闪烁步长。

        private void Timer_EnterPrompt_Tick(object sender, EventArgs e)
        {
            //
            // Timer_EnterPrompt。
            //

            if (EnterPrompt_Val >= 0 && EnterPrompt_Val <= 1)
            {
                EnterPrompt_Val += EnterPrompt_Step;
            }

            if (EnterPrompt_Val < 0 || EnterPrompt_Val > 1)
            {
                EnterPrompt_Val = Math.Max(0, Math.Min(EnterPrompt_Val, 1));

                EnterPrompt_Step = -EnterPrompt_Step;
            }

            EnterGameBackColor_INC = Com.ColorManipulation.BlendByRGB(Me.RecommendColors.Border_INC, Me.RecommendColors.Border, EnterPrompt_Val).ToColor();

            //

            if (Label_ContinueLastGame.Visible)
            {
                Label_StartNewGame.Top = 0;

                if (Com.Geometry.CursorIsInControl(Label_StartNewGame))
                {
                    Label_StartNewGame.Height = Math.Max(EnterGameButtonHeight_Min, Math.Min(EnterGameButtonHeight_Max, Label_StartNewGame.Height + Math.Max(1, (EnterGameButtonHeight_Max - Label_StartNewGame.Height) / 4)));
                }
                else
                {
                    Label_StartNewGame.Height = Math.Max(EnterGameButtonHeight_Min, Math.Min(EnterGameButtonHeight_Max, Label_StartNewGame.Height - Math.Max(1, (Label_StartNewGame.Height - EnterGameButtonHeight_Min) / 4)));
                }

                Label_ContinueLastGame.Top = Label_StartNewGame.Bottom;
                Label_ContinueLastGame.Height = Panel_EnterGameSelection.Height - Label_ContinueLastGame.Top;
            }
            else
            {
                Label_StartNewGame.Height = EnterGameButtonHeight_Max;

                Label_StartNewGame.Top = (Panel_EnterGameSelection.Height - Label_StartNewGame.Height) / 2;
            }

            Label_StartNewGame.Width = (Int32)(Math.Sqrt((double)Label_StartNewGame.Height / EnterGameButtonHeight_Max) * Panel_EnterGameSelection.Width);
            Label_StartNewGame.Left = (Panel_EnterGameSelection.Width - Label_StartNewGame.Width) / 2;

            Label_ContinueLastGame.Width = (Int32)(Math.Sqrt((double)Label_ContinueLastGame.Height / EnterGameButtonHeight_Max) * Panel_EnterGameSelection.Width);
            Label_ContinueLastGame.Left = (Panel_EnterGameSelection.Width - Label_ContinueLastGame.Width) / 2;

            Label_StartNewGame.Font = new Font("微软雅黑", Math.Max(1F, (Label_StartNewGame.Height - 4) / 3F), FontStyle.Regular, GraphicsUnit.Point, 134);
            Label_ContinueLastGame.Font = new Font("微软雅黑", Math.Max(1F, (Label_ContinueLastGame.Height - 4) / 3F), FontStyle.Regular, GraphicsUnit.Point, 134);

            Label_StartNewGame.ForeColor = Com.ColorManipulation.BlendByRGB(Me.RecommendColors.Text_INC, Me.RecommendColors.Text, Math.Sqrt((double)(Label_StartNewGame.Height - EnterGameButtonHeight_Min) / (EnterGameButtonHeight_Max - EnterGameButtonHeight_Min))).ToColor();
            Label_ContinueLastGame.ForeColor = Com.ColorManipulation.BlendByRGB(Me.RecommendColors.Text_INC, Me.RecommendColors.Text, Math.Sqrt((double)(Label_ContinueLastGame.Height - EnterGameButtonHeight_Min) / (EnterGameButtonHeight_Max - EnterGameButtonHeight_Min))).ToColor();

            //

            Panel_EnterGameSelection.Refresh();
        }

        #endregion

        #region "记录"区域

        private void PaintScore(PaintEventArgs e)
        {
            //
            // 绘制成绩。
            //

            Graphics Grap = e.Graphics;
            Grap.SmoothingMode = SmoothingMode.AntiAlias;

            //

            Int32 RectBottom = Panel_Score.Height - 50;

            Size RectSize_Max = new Size(Math.Max(2, Panel_Score.Width / 8), Math.Max(2, Panel_Score.Height - 120));
            Size RectSize_Min = new Size(Math.Max(2, Panel_Score.Width / 8), 2);

            Rectangle Rect_This = new Rectangle();
            Rectangle Rect_Best = new Rectangle();

            if (BestRecord.GameTime.TotalMilliseconds == 0 || BestRecord.StepCount == 0)
            {
                Rect_Best.Size = new Size(RectSize_Max.Width, RectSize_Min.Height);
                Rect_This.Size = new Size(Rect_Best.Width, RectSize_Min.Height);
            }
            else
            {
                Record ThRec = new Record();

                if (ThisRecord.Range == Range)
                {
                    ThRec = ThisRecord;
                }

                if (BestRecord.GameTime.TotalMilliseconds >= ThRec.GameTime.TotalMilliseconds)
                {
                    Rect_Best.Size = RectSize_Max;
                    Rect_This.Size = new Size(Rect_Best.Width, (Int32)Math.Max(RectSize_Min.Height, Math.Sqrt(ThRec.GameTime.TotalMilliseconds / BestRecord.GameTime.TotalMilliseconds) * Rect_Best.Height));
                }
                else
                {
                    Rect_This.Size = RectSize_Max;
                    Rect_Best.Size = new Size(Rect_This.Width, (Int32)Math.Max(RectSize_Min.Height, Math.Sqrt(BestRecord.GameTime.TotalMilliseconds / ThRec.GameTime.TotalMilliseconds) * Rect_This.Height));
                }
            }

            Rect_This.Location = new Point((Panel_Score.Width / 2 - Rect_This.Width) / 2, RectBottom - Rect_This.Height);
            Rect_Best.Location = new Point(Panel_Score.Width / 2 + (Panel_Score.Width / 2 - Rect_Best.Width) / 2, RectBottom - Rect_Best.Height);

            Color RectCr = Me.RecommendColors.Border.ToColor();

            GraphicsPath Path_This = new GraphicsPath();
            Path_This.AddRectangle(Rect_This);
            PathGradientBrush PGB_This = new PathGradientBrush(Path_This)
            {
                CenterColor = RectCr,
                SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(RectCr, 0.3) },
                FocusScales = new PointF(0F, 1F)
            };
            Grap.FillPath(PGB_This, Path_This);
            Path_This.Dispose();
            PGB_This.Dispose();

            GraphicsPath Path_Best = new GraphicsPath();
            Path_Best.AddRectangle(Rect_Best);
            PathGradientBrush PGB_Best = new PathGradientBrush(Path_Best)
            {
                CenterColor = RectCr,
                SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(RectCr, 0.3) },
                FocusScales = new PointF(0F, 1F)
            };
            Grap.FillPath(PGB_Best, Path_Best);
            Path_Best.Dispose();
            PGB_Best.Dispose();

            //

            Label_ThisRecordVal_StepCount.Left = Math.Max(0, Math.Min(Panel_Score.Width - Label_ThisRecordVal_StepCount.Width, (Panel_Score.Width / 2 - Label_ThisRecordVal_StepCount.Width) / 2));
            Label_ThisRecordVal_StepCount.Top = Rect_This.Y - 5 - Label_ThisRecordVal_StepCount.Height;
            Label_ThisRecordVal_GameTime.Left = Math.Max(0, Math.Min(Panel_Score.Width - Label_ThisRecordVal_GameTime.Width, (Panel_Score.Width / 2 - Label_ThisRecordVal_GameTime.Width) / 2));
            Label_ThisRecordVal_GameTime.Top = Label_ThisRecordVal_StepCount.Top - Label_ThisRecordVal_GameTime.Height;

            Label_BestRecordVal_StepCount.Left = Math.Max(0, Math.Min(Panel_Score.Width - Label_BestRecordVal_StepCount.Width, Panel_Score.Width / 2 + (Panel_Score.Width / 2 - Label_BestRecordVal_StepCount.Width) / 2));
            Label_BestRecordVal_StepCount.Top = Rect_Best.Y - 5 - Label_BestRecordVal_StepCount.Height;
            Label_BestRecordVal_GameTime.Left = Math.Max(0, Math.Min(Panel_Score.Width - Label_BestRecordVal_GameTime.Width, Panel_Score.Width / 2 + (Panel_Score.Width / 2 - Label_BestRecordVal_GameTime.Width) / 2));
            Label_BestRecordVal_GameTime.Top = Label_BestRecordVal_StepCount.Top - Label_BestRecordVal_GameTime.Height;
        }

        #endregion

        #region "选项"区域

        // 界面布局。

        private void ComboBox_Range_Width_SelectedIndexChanged(object sender, EventArgs e)
        {
            //
            // ComboBox_Range_Width 选中项索引改变。
            //

            Range.Width = Convert.ToInt32(ComboBox_Range_Width.Text);
        }

        private void ComboBox_Range_Height_SelectedIndexChanged(object sender, EventArgs e)
        {
            //
            // ComboBox_Range_Height 选中项索引改变。
            //

            Range.Height = Convert.ToInt32(ComboBox_Range_Height.Text);
        }

        // 样式。

        private void RadioButton_Number_CheckedChanged(object sender, EventArgs e)
        {
            //
            // RadioButton_Number 选中状态改变。
            //

            if (RadioButton_Number.Checked)
            {
                BlockStyle = BlockStyles.Number;
            }

            _ResetBlockStyleControl();
        }

        private void RadioButton_Image_CheckedChanged(object sender, EventArgs e)
        {
            //
            // RadioButton_Image 选中状态改变。
            //

            if (RadioButton_Image.Checked)
            {
                BlockStyle = BlockStyles.Image;
            }

            _ResetBlockStyleControl();
        }

        private void CheckBox_ShowNumberOnImage_CheckedChanged(object sender, EventArgs e)
        {
            //
            // CheckBox_ShowNumberOnImage 选中状态改变。
            //

            ShowNumberOnImage = CheckBox_ShowNumberOnImage.Checked;
        }

        private void _ResetBlockStyleControl()
        {
            //
            // 重置样式控件。
            //

            CheckBox_ShowNumberOnImage.CheckedChanged -= CheckBox_ShowNumberOnImage_CheckedChanged;

            CheckBox_ShowNumberOnImage.Enabled = (BlockStyle == BlockStyles.Image);
            CheckBox_ShowNumberOnImage.Checked = (BlockStyle == BlockStyles.Image && ShowNumberOnImage);

            CheckBox_ShowNumberOnImage.CheckedChanged += CheckBox_ShowNumberOnImage_CheckedChanged;

            Panel_BkgImg.Enabled = Label_SelectBkgImg_Transparent.Visible = Label_SelectBkgImg_WithText.Visible = RadioButton_Image.Checked;
        }

        private void ResetBlockStyleControl()
        {
            //
            // 重置样式控件。
            //

            RadioButton_Number.CheckedChanged -= RadioButton_Number_CheckedChanged;
            RadioButton_Image.CheckedChanged -= RadioButton_Image_CheckedChanged;

            if (BlockStyle == BlockStyles.Image)
            {
                RadioButton_Image.Checked = true;
            }
            else
            {
                RadioButton_Number.Checked = true;
            }

            RadioButton_Number.CheckedChanged += RadioButton_Number_CheckedChanged;
            RadioButton_Image.CheckedChanged += RadioButton_Image_CheckedChanged;

            _ResetBlockStyleControl();
        }

        private void Label_SelectBkgImg_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 Label_SelectBkgImg_Transparent 或 Label_SelectBkgImg_WithText。
            //

            Label_SelectBkgImg_Transparent.BackColor = Me.RecommendColors.Button_DEC.AtAlpha(64).ToColor();
            Label_SelectBkgImg_WithText.BackColor = Me.RecommendColors.Button_DEC.AtAlpha(192).ToColor();
        }

        private void Label_SelectBkgImg_MouseLeave(object sender, EventArgs e)
        {
            //
            // 鼠标离开 Label_SelectBkgImg_Transparent 或 Label_SelectBkgImg_WithText。
            //

            Label_SelectBkgImg_Transparent.BackColor = Color.Transparent;
            Label_SelectBkgImg_WithText.BackColor = Color.FromArgb(192, Panel_BkgImg.BackColor);
        }

        private void Label_SelectBkgImg_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_SelectBkgImg_Transparent 或 Label_SelectBkgImg_WithText。
            //

            if (e.Button == MouseButtons.Left)
            {
                Label_SelectBkgImg_Transparent.BackColor = Me.RecommendColors.Button_INC.AtAlpha(64).ToColor();
                Label_SelectBkgImg_WithText.BackColor = Me.RecommendColors.Button_INC.AtAlpha(192).ToColor();
            }
        }

        private void Label_SelectBkgImg_MouseUp(object sender, MouseEventArgs e)
        {
            //
            // 鼠标释放 Label_SelectBkgImg_Transparent 或 Label_SelectBkgImg_WithText。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (Com.Geometry.CursorIsInControl(Label_SelectBkgImg_Transparent))
                {
                    Label_SelectBkgImg_Transparent.BackColor = Me.RecommendColors.Button_DEC.AtAlpha(64).ToColor();
                    Label_SelectBkgImg_WithText.BackColor = Me.RecommendColors.Button_DEC.AtAlpha(192).ToColor();
                }
                else
                {
                    Label_SelectBkgImg_MouseLeave(sender, e);
                }
            }
        }

        private void Label_SelectBkgImg_MouseClick(object sender, MouseEventArgs e)
        {
            //
            // 鼠标单击 Label_SelectBkgImg_Transparent 或 Label_SelectBkgImg_WithText。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (OpenFileDialog_SelectBkgImg.ShowDialog() == DialogResult.OK)
                {
                    string FilePath = OpenFileDialog_SelectBkgImg.FileName;

                    if (!SelectBkgImg(FilePath))
                    {
                        Me.Enabled = false;

                        MessageBox.Show("无法打开此文件，可能的原因如下:\n\n" + "(1) 不支持的文件格式;\n(2) 图片已损坏;\n(3) 图片分辨率过低;\n(4) 没有足够的访问权限;\n(5) 文件路径或文件名过长，或者包含无效的字符。\n", ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);

                        Me.Enabled = true;
                    }
                }
            }
        }

        // 主题颜色。

        private void RadioButton_UseRandomThemeColor_CheckedChanged(object sender, EventArgs e)
        {
            //
            // RadioButton_UseRandomThemeColor 选中状态改变。
            //

            if (RadioButton_UseRandomThemeColor.Checked)
            {
                UseRandomThemeColor = true;
            }

            Label_ThemeColorName.Enabled = !UseRandomThemeColor;
        }

        private void RadioButton_UseCustomColor_CheckedChanged(object sender, EventArgs e)
        {
            //
            // RadioButton_UseCustomColor 选中状态改变。
            //

            if (RadioButton_UseCustomColor.Checked)
            {
                UseRandomThemeColor = false;
            }

            Label_ThemeColorName.Enabled = !UseRandomThemeColor;
        }

        private void Label_ThemeColorName_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_ThemeColorName。
            //

            ColorDialog_ThemeColor.Color = Me.ThemeColor.ToColor();

            Me.Enabled = false;

            DialogResult DR = ColorDialog_ThemeColor.ShowDialog();

            if (DR == DialogResult.OK)
            {
                Me.ThemeColor = new Com.ColorX(ColorDialog_ThemeColor.Color);
            }

            Me.Enabled = true;
        }

        // 抗锯齿。

        private void CheckBox_AntiAlias_CheckedChanged(object sender, EventArgs e)
        {
            //
            // CheckBox_AntiAlias 选中状态改变。
            //

            AntiAlias = CheckBox_AntiAlias.Checked;
        }

        #endregion

        #region "关于"区域

        private void Label_GitHub_Base_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_GitHub_Base。
            //

            Process.Start(URL_GitHub_Base);
        }

        private void Label_GitHub_Release_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_GitHub_Release。
            //

            Process.Start(URL_GitHub_Release);
        }

        #endregion

    }
}