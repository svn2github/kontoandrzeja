﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using CookComputing.XmlRpc;
using CDBurnerXP;
using CDBurnerXP.IO;
using CDBurnerXP.Forms;

namespace Ketarin.Forms
{
    public partial class ApplicationJobDialog : PersistentForm
    {
        private ApplicationJob m_ApplicationJob = new ApplicationJob();

        #region Properties

        /// <summary>
        /// Gets or sets the ApplicationJob object shown in the dialog.
        /// </summary>
        internal ApplicationJob ApplicationJob
        {
            get {
                return m_ApplicationJob;
            }
            set {
                if (m_ApplicationJob == null)
                {
                    throw new ArgumentNullException("value");
                }

                m_ApplicationJob = value;
                ReadApplication();
                this.Text = "Edit " + m_ApplicationJob.Name;
            }
        }

        public bool ReadOnly 
        {
            get
            {
                return !txtApplicationName.Enabled;
            }
            set
            {
                bool enable = !value;
                txtApplicationName.ReadOnly = value;
                txtCommand.ReadOnly = value;
                txtFixedUrl.ReadOnly = value;
                txtTarget.ReadOnly = value;
                txtSpoofReferer.ReadOnly = value;
                txtFileHippoId.ReadOnly = value;
                cboCategory.Enabled = enable;
                chkDeletePrevious.Enabled = enable;
                chkEnabled.Enabled = enable;
                chkShareOnline.Enabled = enable;
                rbAlwaysDownload.Enabled = false;
                rbBetaAvoid.Enabled = false;
                rbBetaDefault.Enabled = false;
                bBrowseFile.Enabled = enable;
                bOK.Enabled = enable;
                bOK.Visible = enable;
            }
        }

        #endregion

        public ApplicationJobDialog()
        {
            InitializeComponent();
            AcceptButton = bOK;
            CancelButton = bCancel;

            cboCategory.DataSource = DbManager.GetCategories();
        }

        /// <summary>
        /// Reads the ApplicationJob and fills the controls accordingly.
        /// </summary>
        private void ReadApplication()
        {
            txtApplicationName.Text = m_ApplicationJob.Name;
            txtFixedUrl.Text = m_ApplicationJob.FixedDownloadUrl;
            txtTarget.Text = m_ApplicationJob.TargetPath;
            txtFileHippoId.Text = m_ApplicationJob.FileHippoId;
            rbFileHippo.Checked = (m_ApplicationJob.DownloadSourceType == ApplicationJob.SourceType.FileHippo);
            rbFixedUrl.Checked = (m_ApplicationJob.DownloadSourceType == ApplicationJob.SourceType.FixedUrl);
            chkEnabled.Checked = m_ApplicationJob.Enabled;
            rbFolder.Checked = m_ApplicationJob.TargetIsFolder;
            chkDeletePrevious.Checked = m_ApplicationJob.DeletePreviousFile;
            txtCommand.Text = m_ApplicationJob.ExecuteCommand;
            cboCategory.SelectedIndex = -1;
            cboCategory.Text = string.IsNullOrEmpty(m_ApplicationJob.Category) ? null : m_ApplicationJob.Category;
            chkShareOnline.Checked = m_ApplicationJob.ShareApplication;
            chkShareOnline.Enabled = m_ApplicationJob.CanBeShared;
            txtSpoofReferer.Text = m_ApplicationJob.HttpReferer;
            rbBetaAvoid.Checked = (ApplicationJob.DownloadBeta == ApplicationJob.DownloadBetaType.Avoid);
            rbBetaDefault.Checked = (ApplicationJob.DownloadBeta == ApplicationJob.DownloadBetaType.Default);
            rbAlwaysDownload.Checked = (ApplicationJob.DownloadBeta == ApplicationJob.DownloadBetaType.AlwaysDownload);
        }

        /// <summary>
        /// Modifies the ApplicationJob based on the control values.
        /// </summary>
        private void WriteApplication()
        {
            m_ApplicationJob.Name = txtApplicationName.Text;
            m_ApplicationJob.FixedDownloadUrl = txtFixedUrl.Text;
            m_ApplicationJob.TargetPath = txtTarget.Text;
            if (rbFolder.Checked)
            {
                m_ApplicationJob.TargetPath = PathEx.QualifyPath(m_ApplicationJob.TargetPath);
            }
            m_ApplicationJob.Enabled = chkEnabled.Checked;
            m_ApplicationJob.FileHippoId = txtFileHippoId.Text;
            m_ApplicationJob.DeletePreviousFile = chkDeletePrevious.Checked;
            m_ApplicationJob.ExecuteCommand = txtCommand.Text;
            m_ApplicationJob.DownloadSourceType = (rbFixedUrl.Checked) ? ApplicationJob.SourceType.FixedUrl : ApplicationJob.SourceType.FileHippo;
            m_ApplicationJob.Category = cboCategory.Text;
            m_ApplicationJob.ShareApplication = chkShareOnline.Checked;
            m_ApplicationJob.HttpReferer = txtSpoofReferer.Text;
            
            if (rbAlwaysDownload.Checked)
            {
                m_ApplicationJob.DownloadBeta = ApplicationJob.DownloadBetaType.AlwaysDownload;
            }
            else if (rbBetaAvoid.Checked)
            {
                m_ApplicationJob.DownloadBeta = ApplicationJob.DownloadBetaType.Avoid;
            }
            else
            {
                m_ApplicationJob.DownloadBeta = ApplicationJob.DownloadBetaType.Default;
            }
        }

        private void bBrowseFile_Click(object sender, EventArgs e)
        {
            // Depending on the save type, either open a file or directory.

            if (rbFileName.Checked)
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.InitialDirectory = txtTarget.Text;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        txtTarget.Text = dialog.FileName;
                    }
                }
            }
            else
            {
                using (FolderBrowserDialog dialog = new FolderBrowserDialog())
                {
                    // Folder browser doesn't like file names
                    if (File.Exists(txtTarget.Text))
                    {
                        dialog.SelectedPath = Path.GetDirectoryName(txtTarget.Text);
                    }
                    else
                    {
                        dialog.SelectedPath = txtTarget.Text;
                    }

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        txtTarget.Text = dialog.SelectedPath;
                    }
                }
            }
        }

        private void rbFileName_CheckedChanged(object sender, EventArgs e)
        {
            txtTarget.AutoCompleteSource = AutoCompleteSource.FileSystem;
        }

        private void rbDirectory_CheckedChanged(object sender, EventArgs e)
        {
            txtTarget.AutoCompleteSource = AutoCompleteSource.FileSystemDirectories;
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            // Check that a target location is given
            if (string.IsNullOrEmpty(txtTarget.Text))
            {
                MessageBox.Show(this, "You did not specify a target location.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }

            // Check that name is not empty
            if (string.IsNullOrEmpty(txtApplicationName.Text))
            {
                MessageBox.Show(this, "The application name must not be empty.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }

            // Check for valid URL
            if (rbFixedUrl.Checked && string.IsNullOrEmpty(txtFixedUrl.Text))
            {
                MessageBox.Show(this, "The URL must not be empty.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }
            else if (rbFileHippo.Checked && String.IsNullOrEmpty(txtFileHippoId.Text))
            {
                MessageBox.Show(this, "You did not specify a FileHippo ID.\r\nYou can paste the desired URL from the FileHippo.com website, the ID will be extracted automatically.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }

            WriteApplication();

            // All good. If necessary, now start a thread
            // which is going to share the application online.
            ApplicationJob job = this.ApplicationJob;
            if (job.ShareApplication)
            {
                Cursor = Cursors.WaitCursor;

                try
                {
                    IKetarinRpc proxy = XmlRpcProxyGen.Create<IKetarinRpc>();
                    proxy.Timeout = 10000;

                    RpcApplication[] existingApps = proxy.GetSimilarApplications(job.Name, job.Guid.ToString());
                    if (existingApps.Length > 0)
                    {
                        // Prevent similar entries by asking the author
                        // to reconsider his choice of name.
                        SimilarApplicationsDialog dialog = new SimilarApplicationsDialog();
                        dialog.ApplicationJob = job;
                        dialog.Applications = existingApps;
                        if (dialog.ShowDialog(this) != DialogResult.OK)
                        {
                            return;
                        }
                    }

                    // Everything is fine, upload now.
                    Thread thread = new Thread(new ParameterizedThreadStart(ShareOnline));
                    thread.IsBackground = true;
                    thread.Start(job);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private static void ShareOnline(object argument)
        {
            try
            {
                ApplicationJob job = argument as ApplicationJob;
                if (job == null) return;

                IKetarinRpc proxy = XmlRpcProxyGen.Create<IKetarinRpc>();
                proxy.Timeout = 10000;
                proxy.SaveApplication(job.GetXml(), Settings.GetValue("AuthorGuid") as string);
            }
            catch (Exception)
            {
                // No internet, server down, whatever. We don't have to care.
            }
        }

        private void txtFileHippoId_TextChanged(object sender, EventArgs e)
        {
            rbFileHippo.Checked = true;
            // If someone pasted the full URL, extract the ID from it
            Regex regex = new Regex(@"filehippo\.com/download_([0-9a-z._-]+)/", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match id = regex.Match(txtFileHippoId.Text);
            if (id.Groups.Count > 1)
            {
                txtFileHippoId.Text = id.Groups[1].Value;
            }
        }

        private void txtFixedUrl_TextChanged(object sender, EventArgs e)
        {
            rbFixedUrl.Checked = true;
        }

        private void bVariables_Click(object sender, EventArgs e)
        {
            using (EditVariablesDialog dialog = new EditVariablesDialog(m_ApplicationJob))
            {
                dialog.ReadOnly = ReadOnly;
                dialog.ShowDialog(this);
            }
        }

    }
}
