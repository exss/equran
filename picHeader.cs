using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace eQuran {

    public enum IdentityEnum { Quran, Hadeeth };

    
    class picHeader : Control {

        SolidBrush fbrshHeader,fbrshBar,fbrshText;
        ColorPainter fColorPainter;
        Icon Icon;
        IdentityEnum fIdentity;
        const int RECTSMALL_WIDTH = 260;
        Rectangle[] frectIdentity;
        string[] text = { "القرآن الكريم", "الأحاديث النبوية" };
        Font ffontIdentity = new Font("ae_AlArabiya", 14);                           
        StringFormat FlagRL = new StringFormat(StringFormatFlags.DirectionRightToLeft);
        int[] textWidth = new int[2];
        Rectangle frectView, frectHeader, frectBar;

        public event EventHandler SelectedIdentityChanged;



        public picHeader() {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            fbrshHeader = new SolidBrush(Color.FromArgb(40, 85, 51));
            fbrshBar = new SolidBrush(Color.FromArgb(26, 54, 26));
            fbrshText = new SolidBrush(Color.White);
            Font = new Font("Haettenschweiler", 30);
            Icon = eQuran.Properties.Resources.header_icon;
            
            fIdentity = IdentityEnum.Quran;
            frectIdentity = new Rectangle[2];
            Graphics g = CreateGraphics();                        
            textWidth[0] = (int)g.MeasureString(text[0], ffontIdentity).Width;
            textWidth[1] = (int)g.MeasureString(text[1], ffontIdentity).Width;

        }


        public IdentityEnum SelectedIdentity {
            get {
                return fIdentity;
            }
            set {
                fIdentity = value;
                Invalidate();
            }

        }
        
        public ColorPainter ColorPainter {
            get { return fColorPainter; }
            set {
                if (value is ColorPainter) {
                    fColorPainter = value;
                    fbrshText.Color = fColorPainter.HeaderText;
                    fbrshBar.Color = fColorPainter.BarColor;
                    fbrshHeader.Color = fColorPainter.HeaderColor;
                    fColorPainter.ColorChanged += new ColorPainter.ColorsChangedEventHandler(OnColorPainterChanged);
                } else fColorPainter = null;
            }
        }

        private void OnColorPainterChanged(object Sender) {
            fbrshText.Color = fColorPainter.HeaderText;
            fbrshBar.Color = fColorPainter.BarColor;
            fbrshHeader.Color = fColorPainter.HeaderColor;            
            Invalidate();
        }


        protected override void OnResize(EventArgs e) {

            frectHeader = new Rectangle(0, 0, ClientRectangle.Width, ((int)(ClientRectangle.Height * 0.65)));
            frectBar = new Rectangle(8,
                                     frectHeader.Bottom,
                                     ClientRectangle.Width - RECTSMALL_WIDTH - 10,
                                     ((int)(ClientRectangle.Height * 0.35)));
            
            
            frectView = new Rectangle(frectBar.Right + 10,
                                      frectHeader.Bottom,
                                      RECTSMALL_WIDTH - 16,
                                      frectBar.Height);            
            frectIdentity[0] = new Rectangle(frectView.Right - textWidth[0] - 5, frectView.Top, textWidth[0], frectView.Height);
            frectIdentity[1] = new Rectangle(frectIdentity[0].Left - textWidth[1] , frectView.Top, textWidth[1], frectView.Height);

            
            Invalidate(ClientRectangle);
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs pe) {
            Graphics g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            /* Draw all the layout */
            GraphicsPath grpBar = RoundedRectanglePath(frectBar, 12);
            g.FillPath(fbrshBar, grpBar);            
            g.FillRectangle(fbrshHeader, frectHeader);

            grpBar = RoundedRectanglePath(frectView, 12);
            g.FillPath(fbrshBar, grpBar);
            g.FillRectangle(fbrshHeader, frectHeader);

            
            g.DrawString("eQURAN", Font, fbrshText , 15 + Icon.Width, 2);
            g.DrawIcon(Icon, 14, 8);

            /* Draw the selected text and effects */
            g.DrawString(text[0], ffontIdentity, fbrshText, frectIdentity[0].Right, frectView.Top, FlagRL);
            g.DrawString(text[1], ffontIdentity, fbrshText, frectIdentity[1].Right, frectView.Top, FlagRL);
            
            switch (fIdentity) {
                case IdentityEnum.Quran:
                    g.FillRectangle(fbrshText, frectIdentity[0].X, frectIdentity[0].Top -5 , frectIdentity[0].Width,5);
                    break;
                case IdentityEnum.Hadeeth:
                    g.FillRectangle(fbrshText, frectIdentity[1].X, frectIdentity[0].Top - 5, frectIdentity[0].Width, 5);
                    break;
            }
            
            
        }


        protected override void OnMouseMove(MouseEventArgs e) {

            if (frectIdentity[0].Contains(e.Location)) Cursor = Cursors.Hand;
            else if (frectIdentity[1].Contains(e.Location)) Cursor = Cursors.Hand;
            else Cursor = Cursors.Default;
            
            base.OnMouseMove(e);
        }

        protected override void OnMouseClick(MouseEventArgs e) {

            if (frectIdentity[0].Contains(e.Location)) {

                fIdentity = IdentityEnum.Quran;
                SelectedIdentityChanged(this, new EventArgs());
                Invalidate();
            }
            else if (frectIdentity[1].Contains(e.Location)) {                
                fIdentity = IdentityEnum.Hadeeth;
                SelectedIdentityChanged(this, new EventArgs());
                Invalidate();
            }

            base.OnMouseClick(e);
        }

        private static GraphicsPath RoundedRectanglePath(Rectangle rect, int cornerRadius) {

            GraphicsPath roundedRect = new GraphicsPath();

            roundedRect.AddLine(rect.X , rect.Y, rect.Right, rect.Y);

            roundedRect.AddArc(rect.X + rect.Width - cornerRadius * 2, rect.Y + rect.Height - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);

            roundedRect.AddLine(rect.Right - cornerRadius * 2, rect.Bottom, rect.X + cornerRadius * 2, rect.Bottom);
            roundedRect.AddArc(rect.X, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
            roundedRect.AddLine(rect.X, rect.Bottom - cornerRadius * 2, rect.X, rect.Y + cornerRadius * 2);
            roundedRect.CloseFigure();
            return roundedRect;

        } 


	}
	
}
