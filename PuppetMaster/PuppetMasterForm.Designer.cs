﻿namespace SESDAD
{
    partial class PuppetMasterForm
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
            this.tb_Command = new System.Windows.Forms.TextBox();
            this.bt_Command = new System.Windows.Forms.Button();
            this.tb_Script = new System.Windows.Forms.TextBox();
            this.bt_Script = new System.Windows.Forms.Button();
            this.tb_Log = new System.Windows.Forms.TextBox();
            this.lb_Command = new System.Windows.Forms.Label();
            this.lb_Script = new System.Windows.Forms.Label();
            this.lb_Log = new System.Windows.Forms.Label();
            this.tb_ClearLog = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tb_Command
            // 
            this.tb_Command.Location = new System.Drawing.Point(20, 55);
            this.tb_Command.Name = "tb_Command";
            this.tb_Command.Size = new System.Drawing.Size(414, 20);
            this.tb_Command.TabIndex = 0;
            // 
            // bt_Command
            // 
            this.bt_Command.Location = new System.Drawing.Point(450, 55);
            this.bt_Command.Name = "bt_Command";
            this.bt_Command.Size = new System.Drawing.Size(75, 23);
            this.bt_Command.TabIndex = 1;
            this.bt_Command.Text = "Run";
            this.bt_Command.UseVisualStyleBackColor = true;
            this.bt_Command.Click += new System.EventHandler(this.bt_Command_Click);
            // 
            // tb_Script
            // 
            this.tb_Script.Location = new System.Drawing.Point(20, 120);
            this.tb_Script.Multiline = true;
            this.tb_Script.Name = "tb_Script";
            this.tb_Script.Size = new System.Drawing.Size(216, 436);
            this.tb_Script.TabIndex = 2;
            // 
            // bt_Script
            // 
            this.bt_Script.Location = new System.Drawing.Point(20, 562);
            this.bt_Script.Name = "bt_Script";
            this.bt_Script.Size = new System.Drawing.Size(216, 23);
            this.bt_Script.TabIndex = 3;
            this.bt_Script.Text = "Run Script";
            this.bt_Script.UseVisualStyleBackColor = true;
            this.bt_Script.Click += new System.EventHandler(this.bt_Script_Click);
            // 
            // tb_Log
            // 
            this.tb_Log.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.tb_Log.Location = new System.Drawing.Point(264, 120);
            this.tb_Log.Multiline = true;
            this.tb_Log.Name = "tb_Log";
            this.tb_Log.ReadOnly = true;
            this.tb_Log.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tb_Log.Size = new System.Drawing.Size(559, 465);
            this.tb_Log.TabIndex = 4;
            // 
            // lb_Command
            // 
            this.lb_Command.AutoSize = true;
            this.lb_Command.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lb_Command.Location = new System.Drawing.Point(20, 23);
            this.lb_Command.Name = "lb_Command";
            this.lb_Command.Size = new System.Drawing.Size(114, 17);
            this.lb_Command.TabIndex = 5;
            this.lb_Command.Text = "Single Command";
            // 
            // lb_Script
            // 
            this.lb_Script.AutoSize = true;
            this.lb_Script.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lb_Script.Location = new System.Drawing.Point(20, 97);
            this.lb_Script.Name = "lb_Script";
            this.lb_Script.Size = new System.Drawing.Size(71, 17);
            this.lb_Script.TabIndex = 6;
            this.lb_Script.Text = "Script Box";
            // 
            // lb_Log
            // 
            this.lb_Log.AutoSize = true;
            this.lb_Log.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lb_Log.Location = new System.Drawing.Point(270, 97);
            this.lb_Log.Name = "lb_Log";
            this.lb_Log.Size = new System.Drawing.Size(82, 17);
            this.lb_Log.TabIndex = 7;
            this.lb_Log.Text = "System Log";
            // 
            // tb_ClearLog
            // 
            this.tb_ClearLog.Location = new System.Drawing.Point(566, 53);
            this.tb_ClearLog.Name = "tb_ClearLog";
            this.tb_ClearLog.Size = new System.Drawing.Size(257, 23);
            this.tb_ClearLog.TabIndex = 8;
            this.tb_ClearLog.Text = "Clear Log";
            this.tb_ClearLog.UseVisualStyleBackColor = true;
            this.tb_ClearLog.Click += new System.EventHandler(this.tb_ClearLog_Click);
            // 
            // PuppetMasterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(843, 594);
            this.Controls.Add(this.tb_ClearLog);
            this.Controls.Add(this.lb_Log);
            this.Controls.Add(this.lb_Script);
            this.Controls.Add(this.lb_Command);
            this.Controls.Add(this.tb_Log);
            this.Controls.Add(this.bt_Script);
            this.Controls.Add(this.tb_Script);
            this.Controls.Add(this.bt_Command);
            this.Controls.Add(this.tb_Command);
            this.Name = "PuppetMasterForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tb_Command;
        private System.Windows.Forms.Button bt_Command;
        private System.Windows.Forms.TextBox tb_Script;
        private System.Windows.Forms.Button bt_Script;
        private System.Windows.Forms.TextBox tb_Log;
        private System.Windows.Forms.Label lb_Command;
        private System.Windows.Forms.Label lb_Script;
        private System.Windows.Forms.Label lb_Log;
        private System.Windows.Forms.Button tb_ClearLog;
    }
}

