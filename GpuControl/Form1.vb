Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports MSI.Afterburner
Imports MSI.Afterburner.Exceptions
Imports System.Net.Sockets
Imports System.Text
Imports System.Security.Cryptography.X509Certificates
Imports System.Security.Authentication
Imports System.Net.Security
Public Class Form1
    Dim obj As New HardwareMonitor
    Dim aTabToGPU As New List(Of Integer)

    Dim clientSocket As New System.Net.Sockets.TcpClient()
    Dim sslStream As SslStream

    Dim bConnected As Boolean = False
    Dim bRequested As Boolean = False
    Dim sockR As Threading.Thread
    Dim compLabel As String = ""

    Public bRaised As Boolean = False

    Dim iAttempts As Integer = 0

    Private Sub Form1_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        '' Ensures that the threading is killed too.
        End
    End Sub


    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        If My.Settings.authkey = "" Or My.Settings.compName.Trim() = "" Then
            MsgBox("Please enter an auth key and computer name for the server to begin using this program.", MsgBoxStyle.Information, "First Start")
            If Dialog1.ShowDialog() = Windows.Forms.DialogResult.Cancel Then
                MsgBox("You must enter an authKey and computer name to continue." & vbCrLf & "The program will now close down.", MsgBoxStyle.Critical)
                End
            End If
        End If


        Dim obj As New HardwareMonitor

        Dim aGPUs As New List(Of String)
        Dim aTmp As New List(Of String)
        Dim aSpd As New List(Of String)


        For i As Integer = 0 To obj.Header.GpuEntryCount() - 1
            ' RichTextBox1.AppendText("Adding: " & obj.GpuEntries(i).Device.ToString() & vbCrLf)
            aGPUs.Add(obj.GpuEntries(i).Device.ToString())

        Next

        For i As Integer = 0 To obj.Header.EntryCount() - 1
            'RichTextBox1.AppendText("Scanning entry: " & obj.Entries(i).SrcName.ToString() & vbCrLf)
            If obj.Entries(i).SrcName.ToLower().Contains("temp") Then
                aTmp.Add(obj.Entries(i).Data.ToString())
            ElseIf obj.Entries(i).SrcName.ToLower().Contains("fan speed") Then
                aSpd.Add(obj.Entries(i).Data.ToString())
            End If
        Next

        Dim iTab As Integer = 5

        For i As Integer = 0 To aGPUs.Count() - 1
            Dim tmp As New Label
            tmp.Text = aGPUs(i)
            tmp.Parent = FlowLayoutPanel1
            tmp.Show()
            tmp.TabIndex = iTab
            iTab += 1
            aTabToGPU.Add(i)


            Dim tmp2 As New TextBox
            tmp2.Text = "GPU Temp: " & aTmp(i) & " C"
            tmp2.ReadOnly = True
            tmp2.Name = "gputmp" & i
            tmp2.BackColor = SystemColors.Window
            tmp2.Parent = FlowLayoutPanel1
            tmp2.Width = 175
            tmp2.Show()
            tmp2.TabIndex = iTab
            iTab += 1
            aTabToGPU.Add(i)


            Dim tmp3 As New TextBox
            tmp3.Text = "Fan Speed: " & aSpd(i) & "%"
            tmp3.ReadOnly = True
            tmp3.Name = "gpuspd" & i
            tmp3.BackColor = SystemColors.Window
            tmp3.Parent = FlowLayoutPanel1
            tmp3.Width = 175
            tmp3.Show()
            tmp3.TabIndex = iTab
            iTab += 1
            aTabToGPU.Add(i)


            Dim tmp4 As New TrackBar
            AddHandler tmp4.MouseUp, AddressOf TrackBar1_Scroll
            tmp4.Minimum = 10
            tmp4.Maximum = 100
            tmp4.Value = Integer.Parse(aSpd(i))
            tmp4.Width = 175
            tmp4.Name = "gpusld" & i
            tmp4.TickFrequency = 10
            tmp4.Parent = FlowLayoutPanel1
            tmp4.Show()
            tmp4.TabIndex = iTab
            iTab += 1
            aTabToGPU.Add(i)
        Next

        'clientSocket.Connect("127.0.0.1", 8000)

        RunClient(My.Settings.server, "GpuControl")
        sockR = New Threading.Thread(AddressOf performRead)

        sockR.Start()

        Timer1.Enabled = True
        Timer1.Start()
    End Sub

    Public Sub performRead()


        While True

            If Not bConnected Then
                Return
            End If
            Dim s() As String = ReadMessage(sslStream).Trim().Split(" ")

            ' Start reporting temps and fan speed
            If s(0) = "start" Then
                bRequested = True
            ElseIf s(0) = "stop" Then
                bRequested = False
            ElseIf s(0) = "fancontrol" Then

                If Not My.Settings.allowremote Then
                    sendMessage("fancontrol Permission denied!", sslStream)
                    Continue While
                End If

                '' Check the parameters. Must have 2 params (fancontrol gpu speed) and params need
                '' to be within range
                If s.Count < 3 Or s(2) < 20 Or s(2) > 90 Or s(1) < 0 Or s(1) > 6 Then
                    sendMessage("fancontrol Invalid parameters!", sslStream)
                    Continue While
                End If

                Dim obj2 As New ControlMemory
                Try
                    obj2.GpuEntries(s(1)).FanSpeedCur = s(2)
                    obj2.CommitChanges()
                Catch ex As Exception
                    sendMessage("fancontrol Oops, it appears your GPU is set to auto. Please change to auto and try again. (Or maybe an operation is already being sent to the GPU. Please wait for that operation to be sent before sending a new one", sslStream)
                End Try

            End If




        End While
    End Sub
    Private Shared certificateErrors As New Hashtable()

    ' The following method is invoked by the RemoteCertificateValidationDelegate.
    Public Shared Function ValidateServerCertificate(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
        If sslPolicyErrors = sslPolicyErrors.None Then
            Return True
        End If

        ' Console.WriteLine("Certificate error: {0}", sslPolicyErrors)

        ' Do not allow this client to communicate with unauthenticated servers.
        Return True
    End Function
    Public Sub RunClient(ByVal machineName As String, ByVal serverName As String)
        ' Create a TCP/IP client socket.
        ' machineName is the host running the server application.
        ' Dim client As TcpClient = clientSocket
        '(machine,port)
        If bConnected Then
            Return

        End If

        If iAttempts >= 2 Then
            Return
        End If
        Try
            clientSocket = New TcpClient(machineName, 8000)

        Catch ex As Exception
            iAttempts += 1
            status.Text = "Disconnected"
            Return
        End Try


        ' Create an SSL stream that will close the client's stream.
        sslStream = New SslStream(clientSocket.GetStream(), False, New RemoteCertificateValidationCallback(AddressOf ValidateServerCertificate), Nothing)

        ' The server name must match the name on the server certificate.
        Try
            sslStream.AuthenticateAsClient(serverName)
        Catch e As AuthenticationException
            MsgBox("Exception: " & e.Message)
            If e.InnerException IsNot Nothing Then
                MsgBox("Inner exception: " & e.InnerException.Message)
            End If
            MsgBox("Authentication failed - closing the connection.")
            clientSocket.Close()
            Return
        End Try
        ' Encode a test message into a byte array.
        ' Signal the end of the message using the "<EOF>".

        ' Read message from the server.
        sendMessage("hi " & My.Settings.authkey & " " & My.Settings.compName, sslStream)

        Dim serverMessage As String = ReadMessage(sslStream)
        If serverMessage.Split(" ")(1).Trim() = "0" Then
            status.Text = "Failed Authorization"
            bConnected = False
            clientSocket.Close()
            Return

        End If

        bConnected = True

        status.Text = "Connected"

        ' Close the client connection.
    End Sub

    Private Sub sendMessage(ByVal sMessage As String, ByVal sslStream As SslStream)
        Dim messsage() As Byte = Encoding.UTF8.GetBytes(sMessage)
        ' Send hello message to the server. 
        Try
            sslStream.Write(messsage)
            sslStream.Flush()
        Catch ex As Exception
            bConnected = False
            Return
        End Try
    End Sub

    Private Function ReadMessage(ByVal sslStream As SslStream) As String
        ' Read the  message sent by the server.
        ' The end of the message is signaled using the
        ' "<EOF>" marker.

        Dim buffer(2047) As Byte
        Dim messageData As New StringBuilder()
        Dim bytes As Integer = -1


        Do
            Try
                bytes = sslStream.Read(buffer, 0, buffer.Length)
            Catch ex As Exception
                bConnected = False
                Return ""
            End Try

            ' Use Decoder class to convert from bytes to UTF8
            ' in case a character spans two buffers.
            Dim decoder As Decoder = Encoding.UTF8.GetDecoder()
            Dim chars(decoder.GetCharCount(buffer, 0, bytes) - 1) As Char
            decoder.GetChars(buffer, 0, bytes, chars, 0)
            messageData.Append(chars)
            ' Check for EOF.
            If messageData.ToString().IndexOf(Chr(10)) <> -1 Then
                Exit Do
            End If
        Loop While bytes <> 0

        Return messageData.ToString()
    End Function


    Private Sub ExitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        End
    End Sub

    Private Sub AboutToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("GpuControl is a program made by dab using the MSI Library. All rights belong to their respective owners", MsgBoxStyle.Information, "About GpuControl")
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        If Not bConnected And iAttempts < 3 Then
            RunClient(My.Settings.server, "GpuControl")

        End If
        Dim obj As New HardwareMonitor

        Dim aGPUs As New List(Of String)
        Dim aTmp As New List(Of String)
        Dim aSpd As New List(Of String)


        For i As Integer = 0 To obj.Header.GpuEntryCount() - 1
            aGPUs.Add(obj.GpuEntries(i).Device.ToString())
        Next

        For i As Integer = 0 To obj.Header.EntryCount() - 1
            'RichTextBox1.AppendText("Scanning entry: " & obj.Entries(i).SrcName.ToString() & vbCrLf)
            If obj.Entries(i).SrcName.ToLower().Contains("temp") Then
                aTmp.Add(obj.Entries(i).Data.ToString())

            ElseIf obj.Entries(i).SrcName.ToLower().Contains("fan speed") Then
                aSpd.Add(obj.Entries(i).Data.ToString())
            End If
        Next

        If bRequested Then
            Dim sResponse = "{""name"":""" & My.Settings.compName & """,""data"":["
            Dim bFirst As Boolean = True

            For i As Integer = 0 To aGPUs.Count() - 1
                If Not bFirst Then
                    sResponse &= ","
                End If
                bFirst = False
                sResponse &= "[""" & aGPUs(i) & """," & aTmp(i) & "," & aSpd(i) & "]"
            Next
            'For i As Integer = 0 To aGPUs.Count() - 1
            '    If Not bFirst Then
            '        sResponse &= ","
            '    End If
            '    bFirst = False
            '    sResponse &= "[""" & aGPUs(i) & """," & aTmp(i) & "," & aSpd(i) & "]"
            'Next
            sResponse &= "]}"
            sendMessage(sResponse, sslStream)
        End If

        For Each ct As Control In FlowLayoutPanel1.Controls
            If ct.TabIndex >= 5 Then
                Try
                    ' RichTextBox1.AppendText("Tab index to gpu: " & aTabToGPU(ct.TabIndex - 5) & vbCrLf)
                Catch ex As Exception
                    ' RichTextBox1.AppendText("OH HEY! So we are too far from the end of tab to gpu list! I Think...." & vbCrLf)
                    Continue For
                End Try
            End If
            If TypeOf ct Is TextBox Then
                If ct.Name.Contains("tmp") Then
                    Try
                        If aTmp(aTabToGPU(ct.TabIndex - 5)) > My.Settings.raisetemp And My.Settings.autofan Then
                            If bRaised = False Then
                                bRaised = True
                                Dim obj2 As New ControlMemory
                                Try
                                    obj2.GpuEntries(aTabToGPU(ct.TabIndex - 5)).FanSpeedCur = aSpd(aTabToGPU(ct.TabIndex - 5)) + My.Settings.raiseprec
                                    obj2.CommitChanges()
                                Catch ex As Exception
                                    bRaised = False
                                End Try
                            End If

                        Else
                            bRaised = False
                        End If
                        ct.Text = "GPU Temp: " & aTmp(aTabToGPU(ct.TabIndex - 5)) & " C"
                    Catch ex As Exception
                        ' RichTextBox1.AppendText("Error with fan temp!!: & " & ct.TabIndex & ", " & ex.Message & vbCrLf)
                    End Try
                ElseIf ct.Name.Contains("spd") Then
                    Try
                        ct.Text = "Fan speed: " & aSpd(aTabToGPU(ct.TabIndex - 5)) & " %"
                    Catch ex As Exception
                        ' RichTextBox1.AppendText("Error with fan speed!!: & " & ct.TabIndex & ", " & ex.Message & vbCrLf)
                    End Try
                End If
            ElseIf TypeOf ct Is TrackBar Then
                Dim tmp As TrackBar = DirectCast(ct, TrackBar)
                Try
                    tmp.Value = Integer.Parse(aSpd(aTabToGPU(ct.TabIndex - 5)))
                Catch ex As Exception
                    ' RichTextBox1.AppendText("COuldn't set value to: " & ct.TabIndex & ", " & aTabToGPU.Count() & vbCrLf)
                End Try
            End If


        Next

        ' Timer1.Stop()
    End Sub

    Private Sub TrackBar1_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim tmp As TrackBar = DirectCast(sender, TrackBar)

        Dim obj As New ControlMemory
        Try
            obj.GpuEntries(aTabToGPU(tmp.TabIndex - 5)).FanSpeedCur = tmp.Value
            obj.CommitChanges()
        Catch ex As Exception
            MsgBox("Oops, it appears your GPU is set to auto. Please change to auto and try again. (Or maybe an operation is already being sent to the GPU. Please wait for that operation to be sent before sending a new one", MsgBoxStyle.Exclamation, "GpuControl Warning")
        End Try
    End Sub

    Private Sub SettingsToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SettingsToolStripMenuItem.Click
        Dialog1.ShowDialog()
    End Sub

    Private Sub DisconnectToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DisconnectToolStripMenuItem.Click
        If bConnected Then
            iAttempts = 100
            status.Text = "Disconnected"
            bConnected = False
            clientSocket.Close()
        Else
            iAttempts = 0
            RunClient(My.Settings.server, "GpuControl")
        End If
    End Sub
End Class
