using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace XisCoreSensors
{
    public partial class FrmImageManager : Form
    {
        // Propiedades públicas para que el FrmPartViewer pueda pasar y recibir los datos
        public List<string> ImagePaths { get; set; }
        public string ImageSelectorTag { get; set; }

        public FrmImageManager(List<string> currentImagePaths, string currentSelectorTag)
        {
            InitializeComponent();
            ImagePaths = new List<string>(currentImagePaths);
            ImageSelectorTag = currentSelectorTag;

            txtSelectorTag.Text = ImageSelectorTag;
            lstImages.DataSource = ImagePaths;

            if (lstImages.SelectedIndex >= 0)
            {
                PreviewImage();
            }
            else
            {                
                picturePreview.Image = null;
                txtSelectorTag.Text = string.Empty;
            }

            RefreshListBox();
        }

        private void btnAddImage_Click(object sender, EventArgs e)
        {
            using(var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() != DialogResult.OK) return;
                ImagePaths.AddRange(ofd.FileNames);
                RefreshListBox();
            }
        }

        private void RefreshListBox()
        {
            string selItem = lstImages.SelectedItem as string;

            lstImages.DataSource = null;
            lstImages.DataSource = ImagePaths;

            if(selItem != null && ImagePaths.Contains(selItem))
            {
                lstImages.SelectedItem = selItem;
            }

            UpdateButtonStates();

        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if(lstImages.SelectedItem is string selectedImage)
            {
                var selIndex = lstImages.SelectedIndex;
                ImagePaths.Remove(selectedImage);
                RefreshListBox();

                if(ImagePaths.Count > 0)
                {
                    lstImages.SelectedIndex = Math.Min(selIndex, ImagePaths.Count - 1);
                }                
            }   
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            ImageSelectorTag = txtSelectorTag.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void lstImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstImages.SelectedIndex >= 0)
            {
                PreviewImage();
                UpdateButtonStates();
            }
            else
            {                
                picturePreview.Image = null;
                txtSelectorTag.Text = string.Empty;
            }
        }

        private void MoveSelectedItem(int direction)
        {
            if (lstImages.SelectedItem == null) return;
            var oldIndex = lstImages.SelectedIndex;
            var newIndex = oldIndex + direction;
            if(newIndex < 0 || newIndex >= ImagePaths.Count) return;
            var item = ImagePaths[oldIndex];
            ImagePaths.RemoveAt(oldIndex);
            ImagePaths.Insert(newIndex, item);
            RefreshListBox();
            lstImages.SelectedIndex = newIndex;
        }

        private void PreviewImage()
        {
            try
            {
                if (lstImages.SelectedItem == null)
                {
                    picturePreview.Image = null;
                    txtSelectorTag.Text = string.Empty;
                    return;
                }
                
                if (picturePreview.Image != null)
                {
                    picturePreview.Image.Dispose();
                }

                var image = Image.FromFile(lstImages.SelectedItem.ToString());
                picturePreview.Image = image;
                txtSelectorTag.Text = lstImages.SelectedIndex.ToString();
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("El archivo de imagen no se encontró.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error al cargar imagen: {e.Message}");
            } 
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(-1);
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(1);
        }

        private void UpdateButtonStates()
        {
            var selIndex = lstImages.SelectedIndex;
            var isItemSelected = selIndex != -1;

            btnRemove.Enabled = isItemSelected;
            btnMoveUp.Enabled = isItemSelected && selIndex > 0;
            btnMoveDown.Enabled = isItemSelected && selIndex < ImagePaths.Count - 1;
        }
    }
}
