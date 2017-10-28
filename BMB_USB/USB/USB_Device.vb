Namespace USB
    MustInherit Class USB_Device
        Const SETUP_STATE_IDLE As Integer = 0
        Const SETUP_STATE_DATA As Integer = 1
        Const SETUP_STATE_ACK As Integer = 2

        Protected addr As Byte
        Protected devname As String

        Dim state As Integer
        Dim setup_buf(8 - 1) As Byte
        Dim data_buf(1024 - 1) As Byte
        Protected remote_wakeup As Integer
        Dim setup_state As Integer
        Dim setup_len As Integer
        Dim setup_index As Integer

        Public Speed As Integer

        Sub New()
        End Sub

        Public Function handle_packet(ByVal pid As Integer, ByVal devaddr As Byte,
                                                   ByVal devep As Byte, ByRef data As Byte(),
                                                   ByVal len As Integer) As Integer
            Dim l As Integer = 0
            Dim ret As Integer = 0

            Select Case pid
                Case USB_MSG_ATTACH
                    state = USB_STATE_ATTACHED
                Case USB_MSG_DETACH
                    state = USB_STATE_NOTATTACHED
                Case USB_MSG_RESET
                    remote_wakeup = 0
                    addr = 0
                    state = USB_STATE_DEFAULT
                    handle_reset()
                Case USB_TOKEN_SETUP
                    If (state < USB_STATE_DEFAULT OrElse devaddr <> addr) Then
                        Return USB_RET_NODEV
                    End If
                    If (len <> 8) Then
                        GoTo fail
                    End If
                    Utils.memcpy(setup_buf, 0, data, 0, 8)

                    setup_len = (Convert.ToInt32(setup_buf(7)) << 8) Or setup_buf(6)
                    setup_index = 0
                    If (setup_buf(0) And USB_DIR_IN) <> 0 Then
                        ret = handle_control((Convert.ToInt32(setup_buf(0)) << 8) Or setup_buf(1),
                                             (Convert.ToInt32(setup_buf(3)) << 8) Or setup_buf(2),
                                             (Convert.ToInt32(setup_buf(5)) << 8) Or setup_buf(4),
                                             setup_len,
                                             data_buf)
                        If (ret < 0) Then
                            Return ret
                        End If
                        If (ret < setup_len) Then
                            setup_len = ret
                        End If
                        setup_state = SETUP_STATE_DATA
                    Else
                        If (setup_len = 0) Then
                            setup_state = SETUP_STATE_ACK
                        Else
                            setup_state = SETUP_STATE_DATA
                        End If
                    End If
                Case USB_TOKEN_IN
                    If (state < USB_STATE_DEFAULT OrElse devaddr <> addr) Then
                        Return USB_RET_NODEV
                    End If
                    Select Case (devep)
                        Case 0
                            Select Case (setup_state)
                                Case SETUP_STATE_ACK
                                    If Not ((setup_buf(0) And USB_DIR_IN) <> 0) Then
                                        setup_state = SETUP_STATE_IDLE

                                        ret = handle_control((Convert.ToInt32(setup_buf(0)) << 8) Or setup_buf(1),
                                                             (Convert.ToInt32(setup_buf(3)) << 8) Or setup_buf(2),
                                                             (Convert.ToInt32(setup_buf(5)) << 8) Or setup_buf(4),
                                                              setup_len,
                                                              data_buf)
                                        If (ret > 0) Then
                                            ret = 0
                                        End If
                                    Else
                                        '/* return 0 byte */
                                    End If
                                Case SETUP_STATE_DATA
                                    If (setup_buf(0) And USB_DIR_IN) <> 0 Then
                                        l = setup_len - setup_index
                                        If (l > len) Then
                                            l = len
                                        End If
                                        'memcpy(data, s->data_buf + s->setup_index, l);
                                        Utils.memcpy(data, 0, data_buf, setup_index, l)
                                        setup_index += l
                                        If (setup_index >= setup_len) Then
                                            setup_state = SETUP_STATE_ACK
                                        End If
                                        ret = l
                                    Else
                                        setup_state = SETUP_STATE_IDLE
                                        GoTo fail
                                    End If
                                Case Else
                                    GoTo fail
                            End Select
                        Case Else
                            ret = handle_data(pid, devep, data, len)
                    End Select
                Case USB_TOKEN_OUT
                    If (state < USB_STATE_DEFAULT OrElse devaddr <> addr) Then
                        Return USB_RET_NODEV
                    End If
                    Select Case (devep)
                        Case 0
                            Select Case (setup_state)
                                Case SETUP_STATE_ACK
                                    If (setup_buf(0) And USB_DIR_IN) <> 0 Then
                                        setup_state = SETUP_STATE_IDLE
                                        '/* transfer OK */
                                    Else
                                        '/* ignore additionnal output */
                                    End If
                                Case SETUP_STATE_DATA
                                    If Not ((setup_buf(0) And USB_DIR_IN) <> 0) Then
                                        l = setup_len - setup_index
                                        If (l > len) Then
                                            l = len
                                        End If
                                        'memcpy(s->data_buf + s->setup_index, data, l);
                                        Utils.memcpy(data_buf, setup_index, data, 0, l)
                                        setup_index += l
                                        If (setup_index >= setup_len) Then
                                            setup_state = SETUP_STATE_ACK
                                        End If
                                        ret = l
                                    Else
                                        setup_state = SETUP_STATE_IDLE
                                        GoTo fail
                                    End If
                                Case Else
                                    GoTo fail
                            End Select
                        Case Else
                            ret = handle_data(pid, devep, data, len)
                    End Select
                Case Else
fail:
                    Log_Error("STALL with pid=0x" & pid.ToString("X") & ", devAddr=" & devaddr & ", devEp=" & devep & ", len=" & len)
                    ret = USB_RET_STALL
            End Select
            Return ret
        End Function

        ''' <summary>
        ''' Called on Shutdown/DestroyDevices
        ''' </summary>
        Public Sub handle_destroy()

        End Sub

        '//might be useful
        Public Overridable Function open(_hWnd As IntPtr) As Integer
            Return 0
        End Function
        Public Overridable Sub close()

        End Sub

        ' /* The following fields are used by the generic USB device
        '    layer. They are here just to avoid creating a new structure for
        '    them. */

        ''' <summary>
        ''' Called on OHCI (re)init, reset device to defualt state
        ''' </summary>
        Public Overridable Sub handle_reset()
            Dim newSB(setup_buf.Count - 1) As Byte
            setup_buf = newSB
            Dim newDB(data_buf.Count - 1) As Byte
            data_buf = newDB
            setup_state = 0
            setup_len = 0
            setup_index = 0
        End Sub
        Protected MustOverride Function handle_control(ByVal request As Integer, ByVal value As Integer, ByVal index As Integer, ByVal length As Integer, ByRef data As Byte()) As Integer
        Protected MustOverride Function handle_data(ByVal pid As Integer, ByVal devep As Byte, ByRef data As Byte(), ByVal len As Integer) As Integer

        '/* XXX: fix overflow */
        Protected Function set_usb_string(ByRef buf As Byte(), ByVal str As String) As Integer
            Dim len, i As Integer
            Dim enc As Text.Encoding = Text.Encoding.ASCII
            Dim strbytes As Byte() = enc.GetBytes(str)

            len = str.Length
            buf(0) = CByte(2 * len + 2)
            buf(1) = 3
            For i = 1 To len
                buf(1 + i) = strbytes(i - 1)
            Next
            Return 1 + len
        End Function

        Public Overridable Sub Freeze(ByRef freezedata As FreezeDataHelper, index As Integer, save As Boolean)
            freezedata.SetByteValue("OHCI.P" & index & ".dev.addr", addr, save)
            freezedata.SetInt32Value("OHCI.P" & index & ".dev.state", state, save)

            'save buffers
            freezedata.SetByteArray("OHCI.P" & index & ".dev.setup_buf", setup_buf, save)
            freezedata.SetByteArray("OHCI.P" & index & ".dev.data_buf", data_buf, save)

            freezedata.SetInt32Value("OHCI.P" & index & ".dev.remote_wakeup", remote_wakeup, save)
            freezedata.SetInt32Value("OHCI.P" & index & ".dev.setup_state", setup_state, save)
            freezedata.SetInt32Value("OHCI.P" & index & ".dev.setup_len", setup_len, save)
            freezedata.SetInt32Value("OHCI.P" & index & ".dev.setup_index", setup_index, save)
        End Sub

        Private Shared Sub Log_Error(str As String)
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.[Error], CInt(USBLogSources.USBDevice), str)
        End Sub
        Private Shared Sub Log_Info(str As String)
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, CInt(USBLogSources.USBDevice), str)
        End Sub
        Private Shared Sub Log_Verb(str As String)
            PSE.CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, CInt(USBLogSources.USBDevice), str)
        End Sub

    End Class
End Namespace
