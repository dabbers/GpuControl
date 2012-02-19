Imports System.Windows.Forms

Public Class Dialog1

    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click

        Form1.bRaised = False
        If compname.Text.Trim() = "" Then
            MsgBox("You must enter a value for computer name!")
            Return
        End If

        My.Settings.raiseprec = precUp.Value
        My.Settings.raisetemp = tempUp.Value
        My.Settings.authkey = authKey.Text
        My.Settings.autofan = autoCheck.Checked
        My.Settings.allowremote = CheckBox1.Checked
        My.Settings.compName = compname.Text
        My.Settings.server = TextBox1.Text






        My.Settings.Save()





        Me.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.Close()
    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub CheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles autoCheck.CheckedChanged
        precUp.Enabled = autoCheck.Checked
        tempUp.Enabled = autoCheck.Checked
    End Sub

    Private Sub Dialog1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        precUp.Value = My.Settings.raiseprec
        tempUp.Value = My.Settings.raisetemp
        authKey.Text = My.Settings.authkey
        autoCheck.Checked = My.Settings.autofan
        CheckBox1.Checked = My.Settings.allowremote

        compname.Text = My.Settings.compName
        precUp.Enabled = autoCheck.Checked
        tempUp.Enabled = autoCheck.Checked
        TextBox1.Text = My.Settings.server
    End Sub
End Class
