using Microsoft.AspNetCore.Components.WebView.WindowsForms;

namespace WinAtomEngineFrontend
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            workSpace = new BlazorWebView();
            SuspendLayout();
            // 
            // workSpace
            // 
            workSpace.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            workSpace.AutoScroll = true;
            workSpace.Location = new Point(1, -1);
            workSpace.Name = "workSpace";
            workSpace.Size = new Size(1260, 712);
            workSpace.TabIndex = 0;
            workSpace.Text = "workSpace";
            workSpace.Click += blazorWebView1_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1262, 713);
            Controls.Add(workSpace);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private BlazorWebView workSpace;
    }
}
