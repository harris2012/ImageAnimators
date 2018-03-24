using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;

namespace Mengliao.CSharp.A14
{
    #region  动画类型枚举

    // 本例为了简单起见没有使用见名知意的名称，如用于实际可将这些枚举成员更名为准确的英文名称
    enum AnimateType
    {
        Animator01, Animator02, Animator03, Animator04, Animator05,
        Animator06, Animator07, Animator08, Animator09, Animator10,
        Animator11, Animator12, Animator13, Animator14, Animator15,
        Animator16, Animator17, Animator18, Animator19, Animator20,
        Animator21, Animator22, Animator23, Animator24, Animator25,
        Animator26, Animator27, Animator28, Animator29, Animator30
    }

    #endregion

    class AnimatorImage
    {
        #region 私有字段

        // 输入位图
        private Bitmap bmp;
        // 是否已经开始绘制
        private bool drawStarted = false;

        // 在绘制过程中是否终止的自动复位信号量，有信号则终止
        private AutoResetEvent cancelEvent = new AutoResetEvent(false);

        // 在绘制过程中是否暂停的手动复位信号量，有信号则暂停
        private ManualResetEvent pauseEvent = new ManualResetEvent(false);

        // 输出位图的DC
        private Graphics dc;

        #endregion

        #region 属性和事件

        private Bitmap outBmp;
        /// <summary>
        /// 输出位图。
        /// </summary>
        public Bitmap OutBmp
        {
            get { return outBmp; }
        }

        private int delay;
        /// <summary>
        /// 延时系数。
        /// </summary>
        public int Delay
        {
            get { return delay; }
            set { delay = Math.Min(Math.Max(1, value), 100); } // 使其介于1到100之间
        }

        /// <summary>
        /// 重绘事件。
        /// </summary>
        public event PaintEventHandler Redraw;
        protected void OnRedraw(Rectangle clipRectangle)
        {
            if (Redraw != null)
            {
                Redraw.Invoke(this, new PaintEventArgs(dc, clipRectangle));
            }
        }

        /// <summary>
        /// 绘制开始事件。
        /// </summary>
        public event EventHandler DrawStarted;
        protected void OnDrawStarted(object sender, EventArgs e)
        {
            drawStarted = true;
            if (DrawStarted != null)
            {
                DrawStarted.Invoke(sender, e);
            }
        }

        /// <summary>
        /// 绘制完成事件。
        /// </summary>
        public event EventHandler DrawCompleted;
        protected void OnDrawCompleted(object sender, EventArgs e)
        {
            drawStarted = false;
            cancelEvent.Reset();
            if (DrawCompleted != null)
            {
                DrawCompleted.Invoke(sender, e);
            }
        }

        #endregion

        #region 私有方法

        // 在输出位图上显示绘制过程中的错误信息
        private void ShowError(string errMsg)
        {
            Font font = new Font("宋体", 9);
            SizeF size = dc.MeasureString(errMsg, font);
            PointF point = new PointF((outBmp.Width - size.Width) / 2f, (outBmp.Height - size.Height) / 2f);
            // 在文字的四个方向各一个像素处绘制其它颜色的文字，以形成边框，否则可能看不清除文字
            dc.DrawString(errMsg, font, Brushes.Red, point.X - 1f, point.Y);
            dc.DrawString(errMsg, font, Brushes.Red, point.X + 1f, point.Y);
            dc.DrawString(errMsg, font, Brushes.Red, point.X, point.Y - 1f);
            dc.DrawString(errMsg, font, Brushes.Red, point.X, point.Y + 1f);
            // 绘制文字
            dc.DrawString(errMsg, font, Brushes.White, point);
            ShowBmp(new Rectangle(Point.Round(point), Size.Round(size)));
        }

        // 供绘制动画方法内部调用，三个重载版本
        private void ShowBmp(Rectangle clipRectangle)
        {
            string cancelMsg = "绘图操作已被用户取消！";
            OnRedraw(clipRectangle);
            if (cancelEvent.WaitOne(0)) // 取消
            {
                // 该异常将被外部方法捕获，即各个绘制方法
                throw new ApplicationException(cancelMsg);
            }
            while (pauseEvent.WaitOne(0)) // 暂停
            {
                if (cancelEvent.WaitOne(10)) // 在暂停期间取消
                {
                    pauseEvent.Reset();
                    throw new ApplicationException(cancelMsg);
                }
            }
        }
        private void ShowBmp(RectangleF clipRectangle) // 接收浮点参数
        {
            ShowBmp(Rectangle.Round(clipRectangle));
        }
        private void ShowBmp() // 重绘全部区域
        {
            ShowBmp(new Rectangle(0, 0, bmp.Width, bmp.Height));
        }

        // 清空背景
        private void ClearBackground()
        {
            dc.Clear(Color.FromKnownColor(KnownColor.ButtonFace)); // 置背景色
            ShowBmp(); // 重绘所有区域
        }

        #endregion

        #region 动画控制

        /// <summary>
        /// 取消绘制。
        /// </summary>
        public void CancelDraw()
        {
            if (drawStarted)
            {
                cancelEvent.Set();
            }
        }

        /// <summary>
        /// 暂停绘制。
        /// </summary>
        public void PauseDraw()
        {
            if (drawStarted)
            {
                pauseEvent.Set();
            }
        }

        /// <summary>
        /// 继续绘制。
        /// </summary>
        public void ResumeDraw()
        {
            if (drawStarted)
            {
                pauseEvent.Reset();
            }
        }

        #endregion

        #region 构造函数
        /// <summary>
        /// 实例化后，需分配事件处理方法，所有事件均在独立线程中触发；默认延时系数为1。
        /// </summary>
        /// <param name="inBmp">输入位图</param>
        public AnimatorImage(Bitmap inBmp)
        {
            delay = 1;
            this.bmp = (Bitmap)inBmp.Clone();
            outBmp = new Bitmap(this.bmp.Width, this.bmp.Height);
            dc = Graphics.FromImage(outBmp);
        }

        #endregion

        #region 绘制动画
        /// <summary>
        /// 以独立线程的方式开始显示动画。
        /// </summary>
        /// <param name="animateType">动画类型枚举</param>
        public void DrawAnimator(AnimateType animateType)
        {
            if (drawStarted) // 判断动画是否已经开始绘制
            {
                if (pauseEvent.WaitOne(0)) // 动画已开始，但被暂停了，继续
                    pauseEvent.Reset();
                else
                    pauseEvent.Set();
                return;
            }
            ThreadStart threadMethod;
            switch (animateType)
            {
                case AnimateType.Animator01:
                    threadMethod = Animator01;
                    break;
                case AnimateType.Animator02:
                    threadMethod = Animator02;
                    break;
                case AnimateType.Animator03:
                    threadMethod = Animator03;
                    break;
                case AnimateType.Animator04:
                    threadMethod = Animator04;
                    break;
                case AnimateType.Animator05:
                    threadMethod = Animator05;
                    break;
                case AnimateType.Animator06:
                    threadMethod = Animator06;
                    break;
                case AnimateType.Animator07:
                    threadMethod = Animator07;
                    break;
                case AnimateType.Animator08:
                    threadMethod = Animator08;
                    break;
                case AnimateType.Animator09:
                    threadMethod = Animator09;
                    break;
                case AnimateType.Animator10:
                    threadMethod = Animator10;
                    break;
                case AnimateType.Animator11:
                    threadMethod = Animator11;
                    break;
                case AnimateType.Animator12:
                    threadMethod = Animator12;
                    break;
                case AnimateType.Animator13:
                    threadMethod = Animator13;
                    break;
                case AnimateType.Animator14:
                    threadMethod = Animator14;
                    break;
                case AnimateType.Animator15:
                    threadMethod = Animator15;
                    break;
                case AnimateType.Animator16:
                    threadMethod = Animator16;
                    break;
                case AnimateType.Animator17:
                    threadMethod = Animator17;
                    break;
                case AnimateType.Animator18:
                    threadMethod = Animator18;
                    break;
                case AnimateType.Animator19:
                    threadMethod = Animator19;
                    break;
                case AnimateType.Animator20:
                    threadMethod = Animator20;
                    break;
                case AnimateType.Animator21:
                    threadMethod = Animator21;
                    break;
                case AnimateType.Animator22:
                    threadMethod = Animator22;
                    break;
                case AnimateType.Animator23:
                    threadMethod = Animator23;
                    break;
                case AnimateType.Animator24:
                    threadMethod = Animator24;
                    break;
                case AnimateType.Animator25:
                    threadMethod = Animator25;
                    break;
                case AnimateType.Animator26:
                    threadMethod = Animator26;
                    break;
                case AnimateType.Animator27:
                    threadMethod = Animator27;
                    break;
                case AnimateType.Animator28:
                    threadMethod = Animator28;
                    break;
                case AnimateType.Animator29:
                    threadMethod = Animator29;
                    break;
                default:
                    threadMethod = Animator30;
                    break;
            }
            Thread drawThread = new Thread(threadMethod);
            drawThread.IsBackground = true; // 设为后台线程，避免该线程未结束时退出主线程而引发异常
            drawThread.Start();
        }

        #endregion

        // ==========
        // 动画算法
        // ==========

        #region 压缩反转（改进版）

        // 原理：计算图像位置和高度，以高度的一半为轴进行对换上下半边的图像
        private void Animator01()
        {
            const float blockSize = 8; // 每次显示的高度增量，应能被高度整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty); // 触发开始绘制事件
                //ClearBackground();

                Color bgColor = Color.FromKnownColor(KnownColor.ButtonFace);
                RectangleF srcRect = new RectangleF(0, 0, bmp.Width, bmp.Height);
                for (float i = (float)Math.Floor(-bmp.Height / blockSize); i <= Math.Ceiling(bmp.Height / blockSize); i++)
                {
                    dc.Clear(bgColor); // 清空DC
                    float j = i * blockSize / 2;
                    float destTop = bmp.Height / 2 - j; // 目标矩形的顶位置
                    // 目标矩形区域在循环的前半段为垂直反向
                    RectangleF destRect = new RectangleF(0, destTop, bmp.Width, 2 * j);
                    // 在指定区域绘制图像，该图像被拉伸
                    dc.DrawImage(bmp, destRect, srcRect, GraphicsUnit.Pixel);

                    ShowBmp();
                    Thread.Sleep(10 * delay); // 休眠
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty); // 触发完成绘制事件
            }
        }

        #endregion

        #region 垂直对接（改进版）

        // 原理：将图像分为上下部分，然后同时向中心移动
        private void Animator02()
        {
            const int stepCount = 4; // 每次上下移动的步长像素，应能被高度整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                Rectangle sourTopRect = new Rectangle(0, 0, bmp.Width, bmp.Height / 2); // 上半部分源区域
                Rectangle sourBottRect = new Rectangle(0, bmp.Height / 2, bmp.Width, bmp.Height / 2); // 下半部分源区域
                for (int i = 0; i <= bmp.Height / 2; i += stepCount)
                {
                    Rectangle destTopRect = new Rectangle(0, i - bmp.Height / 2 + 1, bmp.Width, bmp.Height / 2); // 上半部分目标区域
                    Rectangle destBottRect = new Rectangle(0, bmp.Height - i - 1, bmp.Width, bmp.Height / 2); // 下半部分目标区域
                    dc.DrawImage(bmp, destTopRect, sourTopRect, GraphicsUnit.Pixel);
                    dc.DrawImage(bmp, destBottRect, sourBottRect, GraphicsUnit.Pixel);

                    ShowBmp(Rectangle.Union(destTopRect, destBottRect));
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 中心闭幕（改进版）

        // 原理：由大到小生成图像中心区域，然后用总区域减去该中心区域，并用材质画刷填充
        private void Animator03()
        {
            const float stepCount = 4; // 每次收缩的步长像素
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // 建立空区域，如使用Region的无参构造函数则建立一个无限大小的区域，而非空区域
                Region region = new Region(new GraphicsPath());
                // 建立位图材质画刷
                TextureBrush textureBrush = new TextureBrush(bmp);
                for (float x = 0; x <= bmp.Width / 2f; x += stepCount)
                {
                    // 添加整个位图区域
                    region.Union(new Rectangle(0, 0, bmp.Width, bmp.Height));
                    // 从中心开始，由大到小填充背景色或填充缩小尺寸的原图
                    // 计算高度变化量，如果宽度大，则高度变化量小于宽度，否则大于宽度
                    float y = x * bmp.Height / bmp.Width;
                    RectangleF rect = new RectangleF(x, y, bmp.Width - 2f * x, bmp.Height - 2f * y);
                    // 计算整个位图区域与背景色区域的差集
                    region.Exclude(rect);
                    dc.FillRegion(textureBrush, region); // 使用材质画刷填充区域

                    ShowBmp(region.GetBounds(dc));
                    Thread.Sleep(10 * delay);
                }
                // 由于stepCount可能无法被宽度整除，则最终生成的背景色区域并不为空，故在循环结束后绘制整个位图
                dc.DrawImage(bmp, 0, 0);

                ShowBmp();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 中心放大（改进版）

        // 原理：由中心向边缘按高度和宽度的比例循环输出所有像素，直到高度和宽度为原始大小
        private void Animator04()
        {
            const int stepCount = 4; // 每次增加的像素量，应能被宽度整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                Rectangle sourRect = new Rectangle(0, 0, bmp.Width, bmp.Height); // 源区域为整个位图
                for (int i = 0; i <= bmp.Width / 2; i += stepCount)
                {
                    int j = i * bmp.Height / bmp.Width; // 计算高度变化量，如果宽度大，则高度变化量小于宽度，否则大于宽度
                    Rectangle destRect = new Rectangle(bmp.Width / 2 - i, bmp.Height / 2 - j, 2 * i, 2 * j);
                    dc.DrawImage(bmp, destRect, sourRect, GraphicsUnit.Pixel);

                    ShowBmp(destRect);
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 逐行分块

        // 原理：将图像分为正方形块，然后从左到右，从上到下顺序显示
        private void Animator05()
        {
            const float blockSize = 50; // 正方块的边长
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // 防止最后一列、最后一行不足一块的尺寸而不显示，故采用上取整
                for (int y = 0; y < Math.Ceiling(bmp.Height / blockSize); y++)
                {
                    for (int x = 0; x < Math.Ceiling(bmp.Width / blockSize); x++)
                    {
                        RectangleF rect;
                        if (y % 2 == 0) // 从左到右
                        {
                            rect = new RectangleF(x * blockSize, y * blockSize, blockSize, blockSize);
                        }
                        else // 从右到左
                        {
                            rect = new RectangleF((bmp.Width / blockSize - x - 1) * blockSize, y * blockSize, blockSize, blockSize);
                        }
                        dc.DrawImage(bmp, rect, rect, GraphicsUnit.Pixel);

                        ShowBmp(rect);
                        Thread.Sleep(10 * delay);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 交替分块（改进版）

        // 原理：将图像分为正方形块，然后计算所有分块按照奇偶从左到右显示或从右到左显示所需的区域，并用材质画刷填充
        private void Animator06()
        {
            const float blockSize = 70; // 正方块的边长
            const int showWidth = 1; // 每次显示的像素列数
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // 建立空区域，如使用Region的无参构造函数则建立一个无限大小的区域，而非空区域
                Region region = new Region(new GraphicsPath());
                // 建立位图材质画刷
                TextureBrush textureBrush = new TextureBrush(bmp);
                // 分块的行坐标+列坐标为偶数则从左到右逐列显示本块，否则从右到左逐列显示本块
                for (int i = 0; i <= Math.Ceiling(blockSize / showWidth); i++)
                {
                    for (int x = 0; x < Math.Ceiling(bmp.Width / blockSize); x++)
                    {
                        for (int y = 0; y < Math.Ceiling(bmp.Height / blockSize); y++)
                        {
                            RectangleF rect;
                            // 判断块的行列坐标和为奇数或偶数
                            if ((x + y) % 2 == 0)
                            {
                                rect = new RectangleF(x * blockSize + i * showWidth, y * blockSize, showWidth, blockSize);
                            }
                            else
                            {
                                rect = new RectangleF((x + 1) * blockSize - i * showWidth, y * blockSize, showWidth, blockSize);
                            }
                            region.Union(rect); // 将要显示的区域合并到region中
                        }
                    }
                    dc.FillRegion(textureBrush, region); // 使用材质画刷填充区域

                    ShowBmp(region.GetBounds(dc));
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 交叉竖条（改进版）

        // 原理：将图像分成宽度相等的列，然后计算从上下两个方向交叉前进的区域，并使用材质画刷填充
        private void Animator07()
        {
            const float lineWidth = 4; // 竖条宽度
            const float lineStep = 6; // 竖条每次前进的步长
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                GraphicsPath path = new GraphicsPath(); // 建立路径，路径处理速度要明显快于Region，但不支持集合运算
                TextureBrush textureBrush = new TextureBrush(bmp);
                // 从上到下和从下到上以步长为单位显示
                for (int y = 0; y < Math.Ceiling(bmp.Height / lineStep); y++)
                {
                    // 显示两个方向的每个垂直竖条
                    for (int x = 0; x < Math.Ceiling(bmp.Width / lineWidth); x++)
                    {
                        RectangleF rect;
                        if (x % 2 == 0) // 从上到下
                        {
                            rect = new RectangleF(x * lineWidth, y * lineStep, lineWidth, lineStep);
                        }
                        else // 从下到上
                        {
                            rect = new RectangleF(x * lineWidth, bmp.Height - y * lineStep - lineStep, lineWidth, lineStep);
                        }
                        path.AddRectangle(rect);
                    }
                    dc.FillPath(textureBrush, path);

                    ShowBmp(path.GetBounds());
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 透明淡入（改进版）

        // 原理：使用ImageAttributes类和颜色转换矩阵处理图像，使每个像素的颜色分量同步增加
        private void Animator08()
        {
            const float stepCount = 0.02f; // 颜色转换矩阵增量，该值除1应等于整数
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // ImageAttributes类的实例用于调整颜色，由DrawImage()方法调用
                ImageAttributes attributes = new ImageAttributes();
                // 建立5*5阶RGBA颜色矩阵
                ColorMatrix matrix = new ColorMatrix();
                float value = 0;
                while (value < 1f)
                {
                    matrix.Matrix33 = value;
                    // 为ImageAttributes对象指定颜色调整矩阵
                    // ColorMatrixFlag.Default表示使用矩阵调整所有颜色；ColorAdjustType.Bitmap表示调整位图的颜色
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    dc.Clear(Color.FromKnownColor(KnownColor.ButtonFace)); // 清空DC，否则每次会将不同的透明度图像叠加显示
                    dc.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                    value += stepCount;

                    ShowBmp();
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 三色淡入

        // 原理：使用ImageAttributes类和颜色转换矩阵处理图像，分三次增加每个像素的单个颜色分量
        private void Animator09()
        {
            const float stepCount = 0.025f; // 颜色转换矩阵增量，该值除1应等于整数
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // ImageAttributes类的实例用于调整颜色，由DrawImage()方法调用
                ImageAttributes attributes = new ImageAttributes();
                // 建立5*5阶RGBA颜色矩阵
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix00 = 0f; // R为0
                matrix.Matrix11 = 0f; // G为0
                matrix.Matrix22 = 0f; // B为0
                // 以下三个循环依次处理B、R、G，符合亮度方程的原理
                // 人眼对B最不敏感，对G最敏感，或者说B传达的亮度信息最少，G传达的亮度信息最多
                // 因此先处理亮度信息少的，最后处理亮度信息多的，如果反过来处理，则变化不明显
                float value = 0f;
                while (value < 1f)
                {
                    matrix.Matrix22 = value; // 颜色R的转换矩阵分量值
                    // 为ImageAttributes对象指定颜色调整矩阵
                    // ColorMatrixFlag.Default表示使用矩阵调整所有颜色；ColorAdjustType.Bitmap表示调整位图的颜色
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    dc.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                    value += stepCount;

                    ShowBmp();
                    Thread.Sleep(10 * delay);
                }
                value = stepCount;
                while (value < 1f)
                {
                    matrix.Matrix00 = value; // 颜色G的转换矩阵分量值
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    dc.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                    value += stepCount;

                    ShowBmp();
                    Thread.Sleep(10 * delay);
                }
                value = stepCount;
                while (value < 1f)
                {
                    matrix.Matrix11 = value; // 颜色B的转换矩阵分量值
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    dc.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                    value += stepCount;

                    ShowBmp();
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 水平拉幕

        // 原理：由中心向开始逐渐输出中心两侧的像素，直到宽度为原始大小
        private void Animator10()
        {
            const int stepCount = 4; // 每次增加的步长像素，该值应能被宽度整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                for (int i = 0; i <= Math.Ceiling(bmp.Width / 2f); i += stepCount)
                {
                    Rectangle rect = new Rectangle(bmp.Width / 2 - i, 0, 2 * i, bmp.Height);
                    dc.DrawImage(bmp, rect, rect, GraphicsUnit.Pixel);

                    ShowBmp(rect);
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 随机竖条

        // 原理：将图像分成宽度相等的列，然后随机选择每一列并从上到下显示
        private void Animator11()
        {
            const float lineWidth = 40; // 竖条宽度
            const int stepCount = 12; // 竖条每次前进量
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                Random rnd = new Random(); // 随机数类
                // 生成每个列随机显示的次序
                int[] colIndex = new int[(int)Math.Ceiling(bmp.Width / lineWidth)];
                int index = 1; // 数组索引
                // 数组被自动初始化为0，因此可以通过判断其中的元素是否为0而得知该位置是否产生了随机数
                // 为了区别自动初始化的元素值，index从1开始
                do
                {
                    int s = rnd.Next(colIndex.Length);
                    if (colIndex[s] == 0)
                    {
                        colIndex[s] = index++;
                    }
                } while (index <= colIndex.Length);
                // 按照上面随机生成的次序逐一显示每个竖条
                for (int i = 0; i < colIndex.Length; i++)
                {
                    for (int y = 0; y < bmp.Height; y += stepCount)
                    {
                        RectangleF rect = new RectangleF((colIndex[i] - 1) * lineWidth, y, lineWidth, stepCount);
                        dc.DrawImage(bmp, rect, rect, GraphicsUnit.Pixel);

                        Thread.Sleep(1 * delay);
                        ShowBmp(rect);
                    }

                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 随机拉丝

        // 原理：每次随机显示图像的一个像素行
        private void Animator12()
        {
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                Random rnd = new Random(); // 随机数类
                // 生成每个像素行的显示次序
                int[] rowIndex = new int[bmp.Height];
                int index = 1; // 数组索引
                do
                {
                    int s = rnd.Next(rowIndex.Length);
                    if (rowIndex[s] == 0)
                    {
                        rowIndex[s] = index++;
                    }
                } while (index <= rowIndex.Length);
                // 按照上面随机生成的次序逐一显示每个像素行
                for (int i = 0; i < rowIndex.Length; i++)
                {
                    RectangleF rect = new RectangleF(0, (rowIndex[i] - 1), bmp.Width, 1);
                    dc.DrawImage(bmp, rect, rect, GraphicsUnit.Pixel);

                    ShowBmp(rect);
                    Thread.Sleep(1 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 垂直对切

        // 原理：由图像中心向分左右两半分别向上下两个方向显示，到达边缘后按同样方向补齐另外的部分
        private void Animator13()
        {
            const int stepCount = 4; // 上下前进的增量像素，应能被高度整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // 第一次循环，左半部分从垂直中心向上显示，右半部分从垂直中心向下显示
                for (int y = 0; y <= bmp.Height / 2; y += stepCount)
                {
                    // 左半部分
                    Rectangle rectLeft = new Rectangle(0, bmp.Height / 2 - y - stepCount, bmp.Width / 2, stepCount);
                    dc.DrawImage(bmp, rectLeft, rectLeft, GraphicsUnit.Pixel);
                    // 右半部分
                    Rectangle rectRight = new Rectangle(bmp.Width / 2, bmp.Height / 2 + y, bmp.Width / 2, stepCount);
                    dc.DrawImage(bmp, rectRight, rectRight, GraphicsUnit.Pixel);

                    ShowBmp(Rectangle.Union(rectLeft, rectRight));
                    Thread.Sleep(10 * delay);
                }
                // 第二次循环，左半部分从底边向上显示，右半部分从顶边向下显示
                for (int y = 0; y <= bmp.Height / 2; y += stepCount)
                {
                    // 左半部分
                    Rectangle rectLeft = new Rectangle(0, bmp.Height - y - stepCount, bmp.Width / 2, stepCount);
                    dc.DrawImage(bmp, rectLeft, rectLeft, GraphicsUnit.Pixel);
                    // 右半部分
                    Rectangle rectRight = new Rectangle(bmp.Width / 2, y, bmp.Width / 2, stepCount);
                    dc.DrawImage(bmp, rectRight, rectRight, GraphicsUnit.Pixel);

                    ShowBmp(Rectangle.Union(rectLeft, rectRight));
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 随机分块

        // 原理：框像分割为相等的正方块，然后逐个随机显示其中之一，直到全部显示完成
        private void Animator14()
        {
            const float blockSize = 50; // 分块尺寸，如果该尺寸为4到6时，显示效果为随机图点
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                Random rnd = new Random(); // 随机数类
                // 定义二维数组，对应每个分块，其中保存该块的位置索引（基于总块数）
                int[,] blockIndex = new int[(int)Math.Ceiling(bmp.Width / blockSize), (int)Math.Ceiling(bmp.Height / blockSize)];

                // 生成随机快坐标，并填充顺序号
                int s = 1; // 分块次序（从左到右，从上到下）
                do
                {
                    int x = rnd.Next(blockIndex.GetLength(0));
                    int y = rnd.Next(blockIndex.GetLength(1));
                    if (blockIndex[x, y] == 0)
                    {
                        blockIndex[x, y] = s++;
                    }
                } while (s <= blockIndex.GetLength(0) * blockIndex.GetLength(1));

                // 按照上面随机生成的次序逐一显示所有分块
                for (int x = 0; x < blockIndex.GetLength(0); x++)
                {
                    for (int y = 0; y < blockIndex.GetLength(1); y++)
                    {
                        // blockIndex[x, y]中保存的是分块的显示次序，可以将其转换为对应的坐标
                        RectangleF rect = new RectangleF(((blockIndex[x, y] - 1) % blockIndex.GetLength(0)) * blockSize,
                            ((blockIndex[x, y] - 1) / blockIndex.GetLength(0)) * blockSize, blockSize, blockSize);
                        dc.DrawImage(bmp, rect, rect, GraphicsUnit.Pixel);

                        ShowBmp(rect);
                        Thread.Sleep(10 * delay);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 对角闭幕

        // 原理：每次绘制所有图像内容，然后将不可见的区域生成闭合GraphicsPath并用背景色填充该区域
        private void Animator15()
        {
            const int stepCount = 4; // y轴的增量像素，应能被高度整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // 左上角坐标p0(x0, y0)从左到右，p1(x1, y1)从上到下
                // 右下角坐标p2(x2, y2)从右到左，p3(x3, y3)从下到上
                // 这四个点与左下角点和右上角点构成不可见区域
                PointF p0 = new Point(0, 0);
                PointF p1 = new Point(0, 0);
                PointF p2 = new Point(bmp.Width - 1, bmp.Height - 1);
                PointF p3 = new Point(bmp.Width - 1, bmp.Height - 1);
                // 表示不可见区域的闭合路径
                GraphicsPath path = new GraphicsPath();
                // 以y轴stepCount个像素为增量，也可以使用x轴为增量，一般使用较短的那个轴
                for (int y = 0; y < bmp.Height; y += stepCount)
                {
                    p0.X = y * Convert.ToSingle(bmp.Width) / Convert.ToSingle(bmp.Height); // 以浮点数计算，保证精度
                    p1.Y = y;
                    p2.X = bmp.Width - 1 - p0.X;
                    p3.Y = bmp.Height - 1 - p1.Y;
                    path.Reset();
                    path.AddPolygon(new PointF[] { p0, new PointF(bmp.Width, 0), p3, p2, new PointF(0, bmp.Height), p1 });
                    dc.DrawImage(bmp, 0, 0); // 绘制全部图像
                    dc.FillPath(new SolidBrush(Color.FromKnownColor(KnownColor.ButtonFace)), path); // 填充不可见区域

                    ShowBmp(path.GetBounds());
                    Thread.Sleep(10 * delay);
                }
                // 由于最后一次绘制的不可见区域并不需要，在这里使全部区域可见
                dc.DrawImage(bmp, 0, 0);

                ShowBmp();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 垂直百叶（改进版）

        // 原理：在图像的垂直方向分为高度相等的若干条，然后从上到下计算每次需要显示的区域
        private void Animator16()
        {
            const float lineHeight = 50; // 百叶高度
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                GraphicsPath path = new GraphicsPath();
                TextureBrush textureBrush = new TextureBrush(bmp);
                for (int i = 0; i < lineHeight; i++) // 每条百叶逐行像素显示
                {
                    for (int j = 0; j < Math.Ceiling(bmp.Height / lineHeight); j++)
                    {
                        RectangleF rect = new RectangleF(0, lineHeight * j + i, bmp.Width, 1);
                        path.AddRectangle(rect);
                    }
                    dc.FillPath(textureBrush, path);

                    ShowBmp();
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 压缩竖条（改进版）

        // 原理：在图像的水平方向分为宽度相等的若干条，然后逐一加宽每条竖条的宽度，并在其中显示该条图像的全部内容
        private void Animator17()
        {
            const float lineWidth = 100; // 分条宽度
            const int stepCount = 4; // 每次加宽的步进像素，应能被lineWidth整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                for (int i = 0; i < Math.Ceiling(bmp.Width / lineWidth); i++)
                {
                    for (int j = stepCount; j <= lineWidth; j += stepCount) // 每条宽度逐渐增加，以产生缩放效果
                    {
                        RectangleF sourRect = new RectangleF(lineWidth * i, 0, lineWidth, bmp.Height);
                        RectangleF destRect = new RectangleF(lineWidth * i, 0, j, bmp.Height);
                        dc.DrawImage(bmp, destRect, sourRect, GraphicsUnit.Pixel);

                        ShowBmp(destRect);
                        Thread.Sleep(10 * delay);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 水平拉入（改进版）

        // 原理：由于内存位图与设备无关，故不能使用在水平方向逐渐改变图像分辨率（每英寸点数）的办法
        // 而改为使用在水平方向拉伸显示，并逐步缩小
        private void Animator18()
        {
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                //ClearBackground();

                for (float i = 1; i <= dc.DpiX; i++)
                {
                    RectangleF destRect = new RectangleF(0, 0, bmp.Width * dc.DpiX / i, bmp.Height);
                    RectangleF sourRect = new RectangleF(0, 0, bmp.Width, bmp.Height);
                    dc.DrawImage(bmp, destRect, sourRect, GraphicsUnit.Pixel);

                    ShowBmp();
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 三色对接（改进版）

        // 原理：使用ImageAttributes类和颜色转换矩阵处理图像，首先R和B分别从左右向中心移动，相遇后继续移动，且G从两侧向中间移动，直到相遇
        private void Animator19()
        {
            const int stepCount = 4; // 各个区域每次增加的像素量
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // 建立三个时间段所需的5个不同颜色转换后的位图
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height); // 位图的全部矩形区域
                // 红色分量位图
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix00 = 1f; // R
                matrix.Matrix11 = 0f; // G
                matrix.Matrix22 = 0f; // B
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix); // 使用R分量转换矩阵
                Bitmap redBmp = new Bitmap(bmp.Width, bmp.Height);
                Graphics.FromImage(redBmp).DrawImage(bmp, rect, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                // 蓝色分量位图
                matrix.Matrix00 = 0f; // R
                matrix.Matrix11 = 0f; // G
                matrix.Matrix22 = 1f; // B
                attributes.SetColorMatrix(matrix); // 使用B分量转换矩阵
                Bitmap blueBmp = new Bitmap(bmp.Width, bmp.Height);
                Graphics.FromImage(blueBmp).DrawImage(bmp, rect, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                // 红蓝分量位图
                matrix.Matrix00 = 1f; // R
                matrix.Matrix11 = 0f; // G
                matrix.Matrix22 = 1f; // B
                attributes.SetColorMatrix(matrix); // 使用B分量转换矩阵
                Bitmap redBlueBmp = new Bitmap(bmp.Width, bmp.Height);
                Graphics.FromImage(redBlueBmp).DrawImage(bmp, rect, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                // 红绿分量位图
                matrix.Matrix00 = 1f; // R
                matrix.Matrix11 = 1f; // G
                matrix.Matrix22 = 0f; // B
                attributes.SetColorMatrix(matrix); // 使用B分量转换矩阵
                Bitmap redGreenBmp = new Bitmap(bmp.Width, bmp.Height);
                Graphics.FromImage(redGreenBmp).DrawImage(bmp, rect, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                // 蓝绿分量位图
                matrix.Matrix00 = 0f; // R
                matrix.Matrix11 = 1f; // G
                matrix.Matrix22 = 1f; // B
                attributes.SetColorMatrix(matrix); // 使用B分量转换矩阵
                Bitmap blueGreenBmp = new Bitmap(bmp.Width, bmp.Height);
                Graphics.FromImage(blueGreenBmp).DrawImage(bmp, rect, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);


                // 第1段：1/2时间（设从左到右时间为1），R和B分别从左右向中间移动，在1/2处相遇
                for (int x = 0; x < bmp.Width / 2; x += stepCount)
                {
                    Rectangle rectR = new Rectangle(x, 0, stepCount, bmp.Height); // R的区域，从左到右
                    dc.DrawImage(redBmp, rectR, rectR, GraphicsUnit.Pixel);

                    Rectangle rectB = new Rectangle(bmp.Width - x - stepCount, 0, stepCount, bmp.Height); // B的区域，从右到左
                    dc.DrawImage(blueBmp, rectB, rectB, GraphicsUnit.Pixel);

                    ShowBmp(Rectangle.Union(rectR, rectB));
                    Thread.Sleep(10 * delay);
                }

                // 第2段：1/4时间，R和B从中间分别向右、左移动，G从左右向中间移动，在1/4和3/4处相遇
                ColorMatrix matrixGLeft = new ColorMatrix(); // 处理从左到右的G
                ColorMatrix matrixGRight = new ColorMatrix(); // 处理从右到左的G
                for (int x = 0; x < bmp.Width / 4; x += stepCount)
                {
                    Rectangle rectBR = new Rectangle(bmp.Width / 2 - x - stepCount, 0, 2 * (x + stepCount), bmp.Height); // B和R的区域（位于中心）
                    dc.DrawImage(redBlueBmp, rectBR, rectBR, GraphicsUnit.Pixel);

                    Rectangle rectGLeft = new Rectangle(x, 0, stepCount, bmp.Height); // 左侧G的区域，从左到右
                    dc.DrawImage(redGreenBmp, rectGLeft, rectGLeft, GraphicsUnit.Pixel);

                    Rectangle rectGRight = new Rectangle(bmp.Width - x - stepCount, 0, stepCount, bmp.Height); // 右侧G的区域，从右到左
                    dc.DrawImage(blueGreenBmp, rectGRight, rectGRight, GraphicsUnit.Pixel);

                    ShowBmp(Rectangle.Union(Rectangle.Union(rectBR, rectGLeft), rectGRight));
                    Thread.Sleep(10 * delay);
                }

                // 第3段：1/4时间，显示全色，在1/4处同时向左右两侧扩展（即每次左右各扩展stepCount像素），3/4处与1/4处相同
                for (int x = 0; x < bmp.Width / 4; x += stepCount)
                {
                    Rectangle rect1_4 = new Rectangle(bmp.Width / 4 - x - stepCount, 0, 2 * (x + stepCount), bmp.Height); // 1/4处的区域
                    dc.DrawImage(bmp, rect1_4, rect1_4, GraphicsUnit.Pixel);

                    Rectangle rect3_4 = new Rectangle(bmp.Width / 4 * 3 - x - stepCount, 0, 2 * (x + stepCount), bmp.Height); // 3/4处的区域
                    dc.DrawImage(bmp, rect3_4, rect3_4, GraphicsUnit.Pixel);

                    ShowBmp(Rectangle.Union(rect1_4, rect3_4));
                    Thread.Sleep(10 * delay);
                }

            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 对角滑动（改进版）

        // 原理：在水平方向从右到左，在垂直方向从下到上移动图像
        private void Animator20()
        {
            const int movePixel = 4; // 每次移动的像素，应能被图像高度整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                RectangleF sourRect = new RectangleF(0, 0, bmp.Width, bmp.Height);
                for (int y = bmp.Height; y >= 0; y -= movePixel) // 从下到上移动图像
                {
                    // 按比例计算水平方向移动的量
                    RectangleF destRect = new RectangleF(y * Convert.ToSingle(bmp.Width) / bmp.Height, y, bmp.Width, bmp.Height);
                    dc.DrawImage(bmp, destRect, sourRect, GraphicsUnit.Pixel);

                    ShowBmp(destRect);
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 旋转放大

        // 原理：使用Graphics的RotateTransform()方法进行坐标变换
        private void Animator21()
        {
            const float anglePer = 6; // 每次旋转的角度，应能被360整除
            const int roundCount = 2; // 旋转周数
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                //ClearBackground();

                for (float angle = anglePer; angle <= 360 * roundCount; angle += anglePer) // 每次旋转若干度度，同时进行缩放
                {
                    dc.Clear(Color.FromKnownColor(KnownColor.ButtonFace)); // 清空DC的内容
                    dc.TranslateTransform(bmp.Width / 2, bmp.Height / 2); // 平移坐标轴，以进行基于图片中心的旋转
                    dc.RotateTransform(angle); // 旋转坐标轴
                    dc.ScaleTransform(angle / 360 / roundCount, angle / 360 / roundCount); // 缩放
                    dc.TranslateTransform(-bmp.Width / 2, -bmp.Height / 2); // 平移坐标轴（复原），用于显示处理后的图像
                    dc.DrawImage(bmp, 0, 0); // 在DC中绘制图像
                    dc.ResetTransform(); // 重置DC的所有变换，准备下一次循环

                    ShowBmp();
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 椭圆拉幕

        // 原理：使用TextureBrush载入图像，然后用Graphics的FillEllipse()方法逐渐扩大显示面积
        private void Animator22()
        {
            const int stepCount = 3; // 椭圆外接矩形的高每次的增量像素
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                TextureBrush textureBrush = new TextureBrush(bmp); // 建立材质画刷
                // 以高度为椭圆由小到大的增量
                // 系数1.5使椭圆加大，直到椭圆的内接矩形等于bmp尺寸
                // 系数1.5为当前长宽比椭圆的估算系数
                for (int i = 1; i <= 1.5 * bmp.Height / 2f; i += stepCount)
                {
                    RectangleF rect = new RectangleF(bmp.Width / 2f - i * bmp.Width / bmp.Height, bmp.Height / 2 - i,
                        2 * i * bmp.Width / bmp.Height, 2 * i); // 在DC中心位置距离椭圆的外接矩形
                    dc.FillEllipse(textureBrush, rect); // 填充

                    ShowBmp(rect);
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 对角拉伸

        // 原理：使用Graphics的DrawImage()方法的一个重载版本，将图像绘制在平行四边形中
        private void Animator23()
        {
            const float stepCount = 4.5f; // 平行四边形左上定点所在矩形对角线的增量
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // 绘制平行四边形的三个点为：左上、右上、左下
                // 平行四边形的右上、左下为固定点，不断改变左上点的坐标，使其趋于矩形
                // 平行四边形的左上点沿矩形对角线（中心到左上）方向改变，故先计算矩形对角线的一半，并以此为循环变量
                double diagonal = Math.Sqrt(Math.Pow(bmp.Width, 2f) + Math.Pow(bmp.Height, 2f)) / 2f; // 矩形对角线的一半
                double a = Math.Atan(Convert.ToDouble(bmp.Height) / bmp.Width); // 计算矩形对角线与底边（Width）的夹角
                for (double i = diagonal; i >= 0; i -= stepCount)
                {
                    // 计算当前左上点的坐标
                    Point point = new Point((int)(Math.Cos(a) * i), (int)(Math.Sin(a) * i));
                    // 生成平行四边形左上、右上、左下坐标点数组
                    Point[] points = { point, new Point(bmp.Width, 0), new Point(0, bmp.Height) };
                    dc.DrawImage(bmp, points); // 将图像绘制在平行四边形中

                    ShowBmp();
                    Thread.Sleep(10 * delay);
                }
                // 平行四边形左上定点的位置最终不一定为(0, 0)，故在循环结束后绘制整个位图
                dc.DrawImage(bmp, 0, 0);

                ShowBmp();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 旋转扫描

        // 原理：使用TextureBrush载入图像，然后用Graphics的FillPie()方法逐渐扩大扇形显示面积
        private void Animator24()
        {
            const int anglePer = 5; // 每次旋转的角度，应能被360整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // 建立椭圆外接矩形区域，该区域的内接椭圆的内接矩形应不小于bmp尺寸
                // 内部使用BitBlt() API函数，大的区域基本不会影响速度
                // 坐标平移后应保证该矩形的内接椭圆中心与bmp中心重合
                Rectangle rect = new Rectangle(-bmp.Width, -bmp.Height, bmp.Width * 3, bmp.Height * 3);
                TextureBrush textureBrush = new TextureBrush(bmp); // 建立材质画刷
                // 以扇形跨越角度（单位度）为增量，即角速度相等，线速度并不相等
                for (int i = 0; i <= 360; i += anglePer)
                {
                    dc.FillPie(textureBrush, rect, 180, i); // 填充扇形

                    ShowBmp(rect);
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 多径扫描

        // 原理：使用TextureBrush载入图像，然后用Graphics的FillPie()方法分多个角度同步显示逐渐扩大的扇形区域
        private void Animator25()
        {
            const float pieCount = 8; // 同时扫描的扇形数量
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // 建立椭圆外接矩形区域，该区域的内接椭圆的内接矩形应不小于bmp尺寸
                // 内部使用BitBlt() API函数，大的区域基本不会影响速度
                // 坐标平移后应保证该矩形的内接椭圆中心与bmp中心重合
                Rectangle rect = new Rectangle(-bmp.Width, -bmp.Height, bmp.Width * 3, bmp.Height * 3);
                TextureBrush textureBrush = new TextureBrush(bmp); // 建立材质画刷
                // 以扇形跨越角度（单位度）为增量，即角速度相等，线速度并不相等
                // 共pieCount个扇形每个扇形共计360/pieCount度
                for (int angle = 1; angle <= Math.Ceiling(360 / pieCount); angle++)
                {
                    // 扫描每个扇形区域
                    for (int i = 0; i < pieCount; i++)
                    {
                        dc.FillPie(textureBrush, rect, i * 360 / pieCount, angle); // 填充扇形
                    }

                    ShowBmp(rect);
                    Thread.Sleep(20 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 随机落幕（改进版）

        // 原理：将图像分成宽度相等的列，然后每次随机若干列前进一定的量，直到所有的竖条均到达图像底端
        private void Animator26()
        {
            const float lineWidth = 15; // 竖条宽度
            const int stepCount = 6; // 竖条每次前进量
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                int colCount = (int)Math.Ceiling(bmp.Width / lineWidth); // 计算列数
                Random rnd = new Random(); // 随机数类
                // 该数组保存每个列前进的位置，即每个列的高度，自动初始化为0
                int[] colIndex = new int[colCount];
                // 按随机次序逐一显示每个竖条
                bool flag = false; // 表示是否所有的列均显示完成，即到达了图像的底边

                Region region = new Region(new GraphicsPath()); // 空区域
                TextureBrush textureBrush = new TextureBrush(bmp); // 建立位图材质画刷
                do // 循环直到所有列均显示完毕，即到达底端
                {
                    for (int i = 0; i < colCount; i++)
                    {
                        int col = rnd.Next(colCount); // 随机生成要显示的列
                        if (colIndex[col] < bmp.Height) // 该列未显示完
                        {
                            colIndex[col] += stepCount; // 记录该列新的位置
                            RectangleF rect = new RectangleF(col * lineWidth, 0, lineWidth, colIndex[col]);
                            region.Union(rect);
                        }
                        else
                        {
                            i--; // 保证每次处理列数为colCount
                        }
                    }
                    dc.FillRegion(textureBrush, region);

                    ShowBmp(region.GetBounds(dc));
                    Thread.Sleep(10 * delay);

                    flag = true; // 假设所有列已显示完成
                    for (int i = 0; i < colIndex.Length; i++)
                    {
                        if (colIndex[i] < bmp.Height)
                        {
                            flag = false; // 存在未显示完的列，仍需循环
                            break;
                        }
                    }
                } while (!flag);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 螺线内旋

        // 原理：从图像左上角开始，以分块大小顺时针内旋显示图像
        private void Animator27()
        {
            const float blockSize = 45; // 正方块的边长
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                int cols = (int)Math.Ceiling(bmp.Width / blockSize); // 按照分块尺寸划分的列总计
                int rows = (int)Math.Ceiling(bmp.Height / blockSize); // 按照分块尺寸划分的行总计
                Point block = new Point(0, 0); // 当前显示块的坐标
                Rectangle area = new Rectangle(0, 0, cols, rows); // 尚未显示分块的区域坐标
                int direction = 0; // 内旋方向，0表示向右，1表示向下，2表示向左，3表示向上
                for (int i = 0; i < cols * rows; i++) // 循环分块总数次
                {
                    RectangleF rect = new RectangleF(block.X * blockSize, block.Y * blockSize, blockSize, blockSize);
                    dc.DrawImage(bmp, rect, rect, GraphicsUnit.Pixel);
                    switch (direction)
                    {
                        case 0: // 当前向右
                            if (block.X < area.Width - 1) // 尚未到达右边界
                            {
                                block.X++; // 继续向右
                            }
                            else // 已到达右边界
                            {
                                direction = 1; // 方向改为向下
                                block.Y++; // 向下
                                area.Y++; // 修改待显示区域的上边界
                            }
                            break;
                        case 1: // 当前向下
                            if (block.Y < area.Height - 1) // 尚未到达下边界
                            {
                                block.Y++; // 继续向下
                            }
                            else // 已到达下边界
                            {
                                direction = 2; // 方向改为向左
                                block.X--; // 向左
                                area.Width--; // 修改待显示区域的右边界
                            }
                            break;
                        case 2: // 当前向左
                            if (block.X > area.X) // 尚未到达左边界
                            {
                                block.X--; // 继续向左
                            }
                            else // 已到达左边界
                            {
                                direction = 3; // 方向改为向上
                                block.Y--; // 向上
                                area.Height--; // 修改待显示区域的下边界
                            }
                            break;
                        default: // 当前向上
                            if (block.Y > area.Y) // 尚未到达上边界
                            {
                                block.Y--; // 继续向上
                            }
                            else // 已到达上边界
                            {
                                direction = 0; // 方向改为向右
                                block.X++; // 向右
                                area.X++; // 修改待显示区域的左边界
                            }
                            break;
                    }

                    ShowBmp(rect);
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 灰度扫描（改进版）

        // 原理：使用ImageAttributes类和颜色转换矩阵处理图像，从下到上灰度显示图像，然后从上到下转换为正片
        private void Animator28()
        {
            const int stepCount = 6; // 每次显示的像素量
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // ImageAttributes类的实例用于调整颜色，由DrawImage()方法调用
                ImageAttributes attributes = new ImageAttributes();
                // 建立5*5阶RGBA颜色矩阵
                ColorMatrix matrix = new ColorMatrix();
                // 根据亮度方程将矩阵设为灰度变换
                for (int i = 0; i < 3; i++)
                {
                    matrix[0, i] = 0.299f;
                    matrix[1, i] = 0.587f;
                    matrix[2, i] = 0.114f;
                }
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                // 建立灰度位图并将输入位图转换为灰度
                Bitmap grayBmp = new Bitmap(bmp.Width, bmp.Height);
                Graphics grayDc = Graphics.FromImage(grayBmp);
                grayDc.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);

                // 以灰度方式从下到上绘制整个图像
                for (int y = bmp.Height - 1; y >= 0; y -= stepCount)
                {
                    Rectangle rect = new Rectangle(0, y, bmp.Width, stepCount);
                    dc.DrawImage(grayBmp, rect, rect, GraphicsUnit.Pixel);

                    ShowBmp(rect);
                    Thread.Sleep(10 * delay);
                }
                // 以正片方式从上到下绘制整个图像
                for (int y = 0; y < bmp.Height; y += stepCount)
                {
                    Rectangle rect = new Rectangle(0, y, bmp.Width, stepCount);
                    dc.DrawImage(bmp, rect, rect, GraphicsUnit.Pixel);

                    ShowBmp(rect);
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 负片追踪（改进版）

        // 原理：使用ImageAttributes类和颜色转换矩阵处理图像，从左到右负片显示图像，同时，以滞后方式显示正片
        private void Animator29()
        {
            const float stepCount = 4; // 每次显示的像素量
            const float delayCount = 280; // 转换为正片的滞后像素数，该值必须能被stepCount整除
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                // ImageAttributes类的实例用于调整颜色，由DrawImage()方法调用
                ImageAttributes attributes = new ImageAttributes();
                // 建立5*5阶RGBA颜色矩阵
                ColorMatrix matrix = new ColorMatrix();
                // 将矩阵设为负片变换
                matrix.Matrix00 = matrix.Matrix11 = matrix.Matrix22 = -1f;
                matrix.Matrix30 = matrix.Matrix31 = matrix.Matrix32 = 1f;
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                // 建立负片图像
                Bitmap negativeBmp = new Bitmap(bmp.Width, bmp.Height);
                // 转换负片图像
                Graphics negativeDc = Graphics.FromImage(negativeBmp);
                negativeDc.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);

                // 以负片方式从左到右绘制整个图像，正片绘制在负片进行到delay时开始，并以合适的速度前进，恰好与负片同时到达右边
                for (int x = 0; x < bmp.Width; x += (int)stepCount)
                {
                    // 负片显示区域
                    RectangleF rect1 = new RectangleF(x, 0, stepCount, bmp.Height);
                    dc.DrawImage(negativeBmp, rect1, rect1, GraphicsUnit.Pixel);
                    RectangleF rect2 = RectangleF.Empty;
                    if (x >= delayCount) // 显示正片区域
                    {
                        // 正片显示区域计算方法：
                        // 正片起始位置：(x - delay) * bmp.Width / (bmp.Width - delay)
                        // 当前负片已显示到delay，还需显示bmp.Width - delay，而正片还需显示bmp.Width
                        // 即负片每显示一次，正片需显示负片的bmp.Width / (bmp.Width - delay)倍
                        // 因此，正片每次的起始位置为(x - delay) * bmp.Width / (bmp.Width - delay)
                        // 正片增量：bmp.Width / ((bmp.Width - delay) / stepCount)
                        // 正片共需显示bmp.Width
                        // 负片还有((bmp.Width - delay) / stepCount)次便显示完成，即正片也有同样的次数
                        // 因此，正片每次显示bmp.Width / ((bmp.Width - delay) / stepCount)
                        rect2 = new RectangleF((x - delayCount) * bmp.Width / (bmp.Width - delayCount), 0,
                            bmp.Width / ((bmp.Width - delayCount) / stepCount), bmp.Height);
                        dc.DrawImage(bmp, rect2, rect2, GraphicsUnit.Pixel);
                    }

                    ShowBmp(RectangleF.Union(rect1, rect2));
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        #region 水平卷轴

        // 原理：从左到右显示逐步图像，在已显示区域的右边缘压缩显示剩余图像
        private void Animator30()
        {
            const float blockWidth = 80; // 压缩图像区域的宽度（像素）
            const float stepCount = 6; // 每次显示步进宽度（像素）
            try
            {
                OnDrawStarted(this, EventArgs.Empty);
                ClearBackground();

                for (int x = 0; x <= Math.Ceiling((bmp.Width - blockWidth) / stepCount); x++)
                {
                    // 绘制不压缩的显示区域
                    RectangleF rect = new RectangleF(x * stepCount, 0, stepCount, bmp.Height);
                    dc.DrawImage(bmp, rect, rect, GraphicsUnit.Pixel);
                    // 绘制压缩区域
                    RectangleF sourRect = new RectangleF((x + 1) * stepCount, 0, bmp.Width - x * stepCount, bmp.Height); // 尚未显示的区域
                    RectangleF destRect = new RectangleF((x + 1) * stepCount, 0, blockWidth, bmp.Height); // 压缩竖条区域
                    dc.DrawImage(bmp, destRect, sourRect, GraphicsUnit.Pixel);

                    ShowBmp(RectangleF.Union(rect, destRect));
                    Thread.Sleep(10 * delay);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                OnDrawCompleted(this, EventArgs.Empty);
            }
        }

        #endregion
    }

}
