Imports BMBUSB.USB
Imports BuzzPluginDriver

Class USB_Buzzer
    Inherits USB_Device

#Region "qemu_keyboard_dev_descriptor"
    Dim qemu_keyboard_dev_descriptor As Byte() = {
    &H12,
    &H1,
    &H10, &H0,
    &H0,
    &H0,
    &H0,
    &H8,
    &H4C, &H5,
    &H0, &H10,
    &H0, &H0,
    &H3,
    &H2,
    &H1,
    &H1}

    '/* u8 bLength; */
    '/* u8 bDescriptorType; Device
    '/* u16 bcdUSB; v1.0 */ 'nuvee has 0x01 0x01
    '/* u8 bDeviceClass; */
    '/* u8 bDeviceSubClass; */
    '/* u8 bDeviceProtocol; [ low/full speeds only ] */
    '/* u8 bMaxPacketSize0; 8 Byte
    '/* u16 idVendor; */ 'nuvee has diffrent values
    '/* u16 idProduct; */ 'nuvee has diffrent values
    '/* u16 bcdDevice */ 'nuvee has diffrent values
    '/* u8 iManufacturer; */ 'nuvee has diffrent values
    '/* u8 iProduct; */ 'nuvee has diffrent values
    '/* u8 iSerialNumber; */ 'nuvee has diffrent values
    '/* u8 bNumConfigurations; */

#End Region

#Region "qemu_keyboard_config_descriptor"
    Dim qemu_keyboard_config_descriptor As Byte() = {
    &H9,
    &H2,
    &H22, &H0,
    &H1,
    &H1,
    &H4,
    &HA0, _
 _
 _
 _
    50, _
 _
 _
 _
    &H9,
    &H4,
    &H0,
    &H0,
    &H1,
    &H3,
    &H1,
    &H1,
    &H5, _
 _
    &H9,
    &H21,
    &H1, &H0,
    0,
    &H1,
    &H22,
    50, 0, _
 _
    &H7,
    &H5,
    &H81,
    &H3,
    &H3, &H0,
    &HA}
    '/* one configuration */
    '/* u8 bLength; */
    '/* u8 bDescriptorType; Configuration */
    '/* u16 wTotalLength; */
    '/* u8 bNumInterfaces; (1) */
    '/* u8 bConfigurationValue; */
    '/* u8 iConfiguration; */
    '/* u8 bmAttributes;                                'nuvee has this set to 0x80
    '    Bit 7: must be set,
    '    6: Self-powered,
    '    5: Remote wakeup,
    '    4..0: resvd */
    '/* u8 MaxPower; */

    '    /* USB 1.1:
    '    * USB 2.0, single TT organization (mandatory):
    '    * one interface, protocol 0
    '    *
    '    * USB 2.0, multiple TT organization (optional):
    '    * two interfaces, protocols 1 (like single TT)
    '    * and 2 (multiple TT mode) ... config is
    '    * sometimes settable
    '    * NOT IMPLEMENTED
    '    */
    '    /* one interface */
    '/* u8 if_bLength; */
    '/* u8 if_bDescriptorType; Interface */
    '/* u8 if_bInterfaceNumber; */
    '/* u8 if_bAlternateSetting; */
    '/* u8 if_bNumEndpoints; */
    '/* u8 if_bInterfaceClass; */
    '/* u8 if_bInterfaceSubClass; */
    '/* u8 if_bInterfaceProtocol; [usb1.1 or single tt] */
    '/* u8 if_iInterface; */
    '    /* HID descriptor */
    '/* u8 bLength; */
    '/* u8 bDescriptorType; */
    '/* u16 HID_class */
    '/* u8 country_code */ 'Look into this
    '/* u8 num_descriptors */
    '/* u8 type; Report */
    '/* u16 len */
    '    /* one endpoint (status change endpoint) */
    '/* u8 ep_bLength; */
    '/* u8 ep_bDescriptorType; Endpoint */
    '/* u8 ep_bEndpointAddress; IN Endpoint 1 */
    '/* u8 ep_bmAttributes; Interrupt */
    '/* u16 ep_wMaxPacketSize; */ 'nuvee has 0x08 0x00 (and now we do)
    '/* u8 ep_bInterval; (255ms -- usb 2.0 spec) */ 'nuvee has 0x08
#End Region

#Region "qemu_keyboard_hid_report_descriptor"
    Dim qemu_keyboard_hid_report_descriptor As Byte() = {
    &H5, &H1,
    &H9, &H6,
    &HA1, &H1,
        &H5, &H7,
        &H19, &HE0,
        &H29, &HE7,
        &H15, &H0,
        &H25, &H1,
        &H75, &H1,
        &H95, &H8,
        &H81, &H2,
        &H95, &H1,
        &H75, &H8,
        &H81, &H3,
        &H95, &H5,
        &H75, &H1,
        &H5, &H8,
        &H19, &H1,
        &H29, &H5,
        &H91, &H2,
        &H95, &H1,
        &H75, &H3,
        &H91, &H3,
        &H95, &H6,
        &H75, &H8,
        &H15, &H0,
        &H25, &H65,
        &H5, &H7,
        &H19, &H0,
        &H29, &H65,
        &H81, &H0,
    &HC0} '// END_COLLECTION

    '    '// USAGE_PAGE (Generic Desktop)
    '    '// USAGE (Keyboard)
    '    '// COLLECTION (Application)
    '    '// USAGE_PAGE (Keyboard)
    '    '// USAGE_MINIMUM (Keyboard Left Control)
    '    '// USAGE_MAXIMUM (Keyboard Right GUI)
    '    '// LOGICAL_MINIMUM (0)
    '    '// LOGICAL_MAXIMUM (1)
    '    '// REPORT_SIZE (1)
    '    '// REPORT_COUNT (8)
    '    '// INPUT (Data,Var,Abs)
    '    '// REPORT_COUNT (1)
    '    '// REPORT_SIZE (8)
    '    '// INPUT (Cnst,Var,Abs)
    '    '// REPORT_COUNT (5)
    '    '// REPORT_SIZE (1)
    '    '// USAGE_PAGE (LEDs)
    '    '// USAGE_MINIMUM (Num Lock)
    '    '// USAGE_MAXIMUM (Kana)
    '    '// OUTPUT (Data,Var,Abs)
    '    '// REPORT_COUNT (1)
    '    '// REPORT_SIZE (3)
    '    '// OUTPUT (Cnst,Var,Abs)
    '    '// REPORT_COUNT (6)
    '    '// REPORT_SIZE (8)
    '    '// LOGICAL_MINIMUM (0)
    '    '// LOGICAL_MAXIMUM (101)
    '    '// USAGE_PAGE (Keyboard)
    '    '// USAGE_MINIMUM (Reserved (no event indicated))
    '    '// USAGE_MAXIMUM (Keyboard Application)
    '    '// INPUT (Data,Ary,Abs)
#End Region

    Const GET_REPORT As Integer = ClassInterfaceRequest Or &H1
    Const GET_IDLE As Integer = ClassInterfaceRequest Or &H2
    Const GET_PROTOCOL As Integer = ClassInterfaceRequest Or &H3
    'Const GET_INTERFACE As Integer = InterfaceRequest Or &HA
    Const SET_REPORT As Integer = ClassInterfaceOutRequest Or &H9
    Const SET_IDLE As Integer = ClassInterfaceOutRequest Or &HA
    Const SET_PROTOCOL As Integer = ClassInterfaceOutRequest Or &HB
    Const SET_INTERFACE As Integer = InterfaceOutRequest Or &H11

    Dim Idle_MaxDuration As Integer = 500

    Dim BuzzerObject As Buzzers
    Dim BuzzerNumber As Integer

    Public Sub New(BuzzerNum As Integer)
        Speed = USB_SPEED_FULL
        BuzzerNumber = BuzzerNum
        BuzzerObject = Buzzers.getBuzzers()
        If BuzzerNumber = 0 Then
            BuzzerObject.USBinit()
        End If
    End Sub

    Protected Overrides Function handle_control(request As Integer, value As Integer, index As Integer, length As Integer, ByRef data() As Byte) As Integer
        Dim ret As Integer = 0
        'Dim buf(8 - 1) As Byte
        Select Case (request)
                'Standard Device Requests
            Case DeviceRequest Or USB_REQ_GET_STATUS
                data(0) = CByte((1 << USB_DEVICE_SELF_POWERED) Or (remote_wakeup << USB_DEVICE_REMOTE_WAKEUP))
                data(1) = &H0
                ret = 2
            Case DeviceOutRequest Or USB_REQ_CLEAR_FEATURE
                If (value = USB_DEVICE_REMOTE_WAKEUP) Then
                    remote_wakeup = 0
                Else
                    GoTo fail
                End If
                ret = 0
            Case DeviceOutRequest Or USB_REQ_SET_FEATURE
                If (value = USB_DEVICE_REMOTE_WAKEUP) Then
                    remote_wakeup = 1
                Else
                    GoTo fail
                End If
                ret = 0
            Case DeviceOutRequest Or USB_REQ_SET_ADDRESS
                addr = CByte(value)
                ret = 0
            Case DeviceRequest Or USB_REQ_GET_DESCRIPTOR
                'value is the descriptor type/id
                Select Case (value >> 8)
                    Case USB_DT_DEVICE
                        Utils.memcpy(data, 0, qemu_keyboard_dev_descriptor, 0,
                                   qemu_keyboard_dev_descriptor.Count)
                        ret = qemu_keyboard_dev_descriptor.Count
                    Case USB_DT_CONFIG
                        Utils.memcpy(data, 0, qemu_keyboard_config_descriptor, 0,
                                   qemu_keyboard_config_descriptor.Count)
                        ret = qemu_keyboard_config_descriptor.Count
                    Case USB_DT_STRING
                        'index is the language ID
                        Select Case (value And &HFF)
                            Case 0
                                '/* language ids */
                                data(0) = 4 'blength in bytes
                                data(1) = 3 'bDescriptorType
                                data(2) = &H9 'wLANGID[0].2
                                data(3) = &H4 'wLANGID[0].1
                                ret = 4
                            Case 1 'see qemu_keyboard_dev_descriptor
                                '/* serial number */
                                ret = set_usb_string(data, "1")
                            Case 2 'see qemu_keyboard_dev_descriptor
                                '/* product description */
                                ret = set_usb_string(data, "Generic USB Keyboard")
                            Case 3 'see qemu_keyboard_dev_descriptor
                                '/* vendor description */
                                ret = set_usb_string(data, "CLR_USB/PCSX2/QEMU")
                            Case 4 'see qemu_keyboard_config_descriptor
                                ret = set_usb_string(data, "HID Keyboard")
                            Case 5 'see qemu_keyboard_config_descriptor /* u8 if_iInterface; */
                                ret = set_usb_string(data, "Endpoint1 Interrupt Pipe")
                            Case Else
                                GoTo fail
                        End Select
                    Case Else
                        GoTo fail
                End Select
            Case DeviceRequest Or USB_REQ_GET_CONFIGURATION
                data(0) = 1
                ret = 1
            Case DeviceOutRequest Or USB_REQ_SET_CONFIGURATION
                'Setting the device config
                'Zero means unconfigured
                '1 is the 1st (and only) config
                If (value <> 1) Then
                    GoTo fail
                End If
                ret = 0
            Case DeviceRequest Or USB_REQ_GET_INTERFACE
                data(0) = 0
                ret = 1
            Case DeviceOutRequest Or USB_REQ_SET_INTERFACE
                ret = 0
                '/* hid specific requests */
            Case DeviceOutRequest Or USB_REQ_GET_DESCRIPTOR
                Select Case (value >> 8)
                    Case &H22
                        Utils.memcpy(data, 0, qemu_keyboard_hid_report_descriptor, 0,
                                   qemu_keyboard_hid_report_descriptor.Count)
                        ret = qemu_keyboard_hid_report_descriptor.Count
                    Case Else
                        GoTo fail
                End Select
                    'Interface requests
            Case SET_PROTOCOL 'Boot vs report protocol
                ret = 0
            Case GET_REPORT
                ret = BuzzerObject.ReadBuzzer(data, BuzzerNumber)
            Case &H2109
                BuzzerObject.WriteBuzzer(data, BuzzerNumber)
                ret = 0
            Case SET_IDLE
                Idle_MaxDuration = (value >> 8) * 4
                ret = 0
            Case Else
fail:
                ret = USB_RET_STALL
        End Select
        Return ret
    End Function

    Protected Overrides Function handle_data(pid As Integer, devep As Byte, ByRef data() As Byte, len As Integer) As Integer
        Dim ret As Integer = 0
        If pid = USB_TOKEN_IN And devep = 1 Then
            ret = BuzzerObject.ReadBuzzer(data, BuzzerNumber)
        End If
        Return ret
    End Function
End Class
