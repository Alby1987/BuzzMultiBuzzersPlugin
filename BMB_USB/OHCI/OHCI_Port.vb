Imports BMBUSB.USB
Namespace OHCI
    Structure OHCI_Port
        Public port As USB_Port
        Public ctrl As UInt32
        Public Sub Freeze(ByRef FreezeData As FreezeDataHelper, index As Integer, save As Boolean)
            FreezeData.SetUInt32Value("OHCI.P" & index & ".ctrl", ctrl, save)
            'save USBPort
            Dim isPort As Boolean = Not IsNothing(port)
            FreezeData.SetBoolValue("OHCI.P" & index & ".port", isPort, save)

            If isPort <> Not IsNothing(port) Then Throw New Exception("USBPort State MissMatch")

            If isPort Then
                port.Freeze(FreezeData, index, save)
            End If
        End Sub
    End Structure
End Namespace
