namespace Exam_proctor
{
    partial class ExamForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.labelName = new System.Windows.Forms.Label();
            this.lableNameValue = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lableStudentIdValue = new System.Windows.Forms.Label();
            this.loggedInAtValue = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(175, 239);
            this.button1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(212, 29);
            this.button1.TabIndex = 0;
            this.button1.Text = "Logout";
            this.button1.UseVisualStyleBackColor = true;
            //this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(76, 176);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(444, 24);
            this.label1.TabIndex = 1;
            this.label1.Text = "You may close this window after your exam finishes.";
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(77, 31);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(0, 13);
            this.labelName.TabIndex = 2;
            // 
            // lableNameValue
            // 
            this.lableNameValue.AutoSize = true;
            this.lableNameValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lableNameValue.Location = new System.Drawing.Point(77, 31);
            this.lableNameValue.Name = "lableNameValue";
            this.lableNameValue.Size = new System.Drawing.Size(60, 24);
            this.lableNameValue.TabIndex = 3;
            this.lableNameValue.Text = "label2";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(253, 55);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 13);
            this.label3.TabIndex = 4;
            // 
            // lableStudentIdValue
            // 
            this.lableStudentIdValue.AutoSize = true;
            this.lableStudentIdValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lableStudentIdValue.Location = new System.Drawing.Point(77, 75);
            this.lableStudentIdValue.Name = "lableStudentIdValue";
            this.lableStudentIdValue.Size = new System.Drawing.Size(60, 24);
            this.lableStudentIdValue.TabIndex = 6;
            this.lableStudentIdValue.Text = "label2";
            // 
            // loggedInAtValue
            // 
            this.loggedInAtValue.AutoSize = true;
            this.loggedInAtValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.loggedInAtValue.Location = new System.Drawing.Point(77, 114);
            this.loggedInAtValue.Name = "loggedInAtValue";
            this.loggedInAtValue.Size = new System.Drawing.Size(60, 24);
            this.loggedInAtValue.TabIndex = 8;
            this.loggedInAtValue.Text = "label4";
            // 
            // ExamForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 366);
            this.Controls.Add(this.loggedInAtValue);
            this.Controls.Add(this.lableStudentIdValue);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lableNameValue);
            this.Controls.Add(this.labelName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "ExamForm";
            this.Text = "ExamForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.Label lableNameValue;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lableStudentIdValue;
        private System.Windows.Forms.Label loggedInAtValue;
    }
}