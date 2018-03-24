using System;
using System.Drawing;
using System.Windows.Forms;

namespace Mengliao.CSharp.A14
{
    public partial class FormMain : Form
    {
        #region 构造函数

        public FormMain()
        {
            InitializeComponent();
            ias = new AnimatorImage(Properties.Resources.img); // 获取资源，实例化动画类
            // 通过委托在不同线程间访问控件
            Action SetGroupBoxEnabled = () => groupBox1.Enabled = true;
            Action SetGroupBoxDisabled = () => groupBox1.Enabled = false;
            ias.DrawStarted += (s, e) => this.Invoke(SetGroupBoxDisabled);
            ias.DrawCompleted += (s, e) => this.Invoke(SetGroupBoxEnabled);
            // Invalidate()方法底层并不涉及控件界面，只是发送消息，因此可以在不同线程间调用，即它是线程安全的
            ias.Redraw += (s, e) => pictureBox1.Invalidate(e.ClipRectangle);
            // 设置速度
            trackBar1.Value = ias.Delay;
        }

        #endregion

        #region 私有字段

        private AnimatorImage ias; // 动画类实例引用
        private AnimateType animatorType = AnimateType.Animator01; // 动画类型

        #endregion

        #region 选择动画类型

        // 所有动画效果选择RadioButton的CheckedChanged事件
        private void SelectAnimator(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            switch (rb.Name)
            {
                case "radioButton1":
                    animatorType = AnimateType.Animator01; // 压缩反转
                    break;
                case "radioButton2":
                    animatorType = AnimateType.Animator02; // 垂直对接
                    break;
                case "radioButton3":
                    animatorType = AnimateType.Animator03; // 中心闭幕
                    break;
                case "radioButton4":
                    animatorType = AnimateType.Animator04; // 中心放大
                    break;
                case "radioButton5":
                    animatorType = AnimateType.Animator05; // 逐行分块
                    break;
                case "radioButton6":
                    animatorType = AnimateType.Animator06; // 交替分块
                    break;
                case "radioButton7":
                    animatorType = AnimateType.Animator07; // 交叉竖条
                    break;
                case "radioButton8":
                    animatorType = AnimateType.Animator08; // 透明淡入
                    break;
                case "radioButton9":
                    animatorType = AnimateType.Animator09; // 三色淡入
                    break;
                case "radioButton10":
                    animatorType = AnimateType.Animator10; // 水平拉幕
                    break;
                case "radioButton11":
                    animatorType = AnimateType.Animator11; // 随机竖条
                    break;
                case "radioButton12":
                    animatorType = AnimateType.Animator12; // 随机拉丝
                    break;
                case "radioButton13":
                    animatorType = AnimateType.Animator13; // 垂直对切
                    break;
                case "radioButton14":
                    animatorType = AnimateType.Animator14; // 随机分块
                    break;
                case "radioButton15":
                    animatorType = AnimateType.Animator15; // 对角闭幕
                    break;
                case "radioButton16":
                    animatorType = AnimateType.Animator16; // 垂直百叶
                    break;
                case "radioButton17":
                    animatorType = AnimateType.Animator17; // 压缩竖条
                    break;
                case "radioButton18":
                    animatorType = AnimateType.Animator18; // 水平拉入
                    break;
                case "radioButton19":
                    animatorType = AnimateType.Animator19; // 三色对接
                    break;
                case "radioButton20":
                    animatorType = AnimateType.Animator20; // 对角滑动
                    break;
                case "radioButton21":
                    animatorType = AnimateType.Animator21; // 旋转放大
                    break;
                case "radioButton22":
                    animatorType = AnimateType.Animator22; // 椭圆拉幕
                    break;
                case "radioButton23":
                    animatorType = AnimateType.Animator23; // 对角拉伸
                    break;
                case "radioButton24":
                    animatorType = AnimateType.Animator24; // 旋转扫描
                    break;
                case "radioButton25":
                    animatorType = AnimateType.Animator25; // 多径扫描
                    break;
                case "radioButton26":
                    animatorType = AnimateType.Animator26; // 随机落幕
                    break;
                case "radioButton27":
                    animatorType = AnimateType.Animator27; // 螺线内旋
                    break;
                case "radioButton28":
                    animatorType = AnimateType.Animator28; // 灰度扫描
                    break;
                case "radioButton29":
                    animatorType = AnimateType.Animator29; // 负片追踪
                    break;
                default:
                    animatorType = AnimateType.Animator30; // 水平卷轴
                    break;
            }
        }

        #endregion

        #region 动画控制

        // 重绘
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // 将动画类中的内存输出位图绘制到DC上
            e.Graphics.DrawImage(ias.OutBmp, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
        }

        // 开始动画
        private void button1_Click_1(object sender, EventArgs e)
        {
            ias.DrawAnimator(animatorType);
        }

        // 暂停动画
        private void button2_Click(object sender, EventArgs e)
        {
            ias.PauseDraw();
        }

        // 继续动画
        private void button3_Click(object sender, EventArgs e)
        {
            ias.ResumeDraw();
        }

        // 取消动画
        private void button4_Click(object sender, EventArgs e)
        {
            ias.CancelDraw();
        }

        // 设置延时系数
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            ias.Delay = trackBar1.Value;
        }

        #endregion
    }
}