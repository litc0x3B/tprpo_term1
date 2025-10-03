namespace LabTemplateFSM
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox textBoxList;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.textBoxList = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBoxList
            // 
            this.textBoxList.AcceptsReturn = true;
            this.textBoxList.AcceptsTab = false;
            this.textBoxList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxList.Font = new System.Drawing.Font("Consolas", 12F);
            this.textBoxList.Location = new System.Drawing.Point(0, 0);
            this.textBoxList.Multiline = true;
            this.textBoxList.Name = "textBoxList";
            this.textBoxList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxList.ShortcutsEnabled = false;
            this.textBoxList.Size = new System.Drawing.Size(784, 461);
            this.textBoxList.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.textBoxList);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
