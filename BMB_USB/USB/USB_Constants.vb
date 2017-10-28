Namespace USB
    Module USB_Constants
        'Public PluginLog As CLR_PluginLog

        Public Const USB_HZ As Long = 12000000
        Public Const PSXCLK As Long = 36864000
        Public Const IOPMEMSIZE As UInt32 = 1024 * 1024 * 2
        Public Const FULL_DEBUG As Boolean = False

        Public Const PLAYER_TWO_PORT As Integer = 0
        Public Const PLAYER_ONE_PORT As Integer = 1

        Public Const USB_TOKEN_SETUP As Integer = &H2D
        Public Const USB_TOKEN_IN As Integer = &H69 '/* device -> host */
        Public Const USB_TOKEN_OUT As Integer = &HE1 '/* host -> device */

        '/* specific usb messages, also sent in the 'pid' parameter */
        Public Const USB_MSG_ATTACH As Integer = &H100
        Public Const USB_MSG_DETACH As Integer = &H101
        Public Const USB_MSG_RESET As Integer = &H102

        Public Const USB_RET_NODEV As Integer = (-1)
        Public Const USB_RET_NAK As Integer = (-2)
        Public Const USB_RET_STALL As Integer = (-3)
        Public Const USB_RET_BABBLE As Integer = (-4)
        Public Const USB_RET_IOERROR As Integer = (-5)

        Public Const USB_SPEED_LOW As Integer = 0
        Public Const USB_SPEED_FULL As Integer = 1
        Public Const USB_SPEED_HIGH As Integer = 2

        Public Const USB_STATE_NOTATTACHED As Integer = 0
        Public Const USB_STATE_ATTACHED As Integer = 1
        '//#define USB_STATE_POWERED     2
        Public Const USB_STATE_DEFAULT As Integer = 3
        '//#define USB_STATE_ADDRESS     4
        '//#define	USB_STATE_CONFIGURED  5
        Public Const USB_STATE_SUSPENDED As Integer = 6

        Public Const USB_DIR_OUT As Integer = 0
        Public Const USB_DIR_IN As Integer = &H80

        Public Const USB_TYPE_MASK As Integer = (&H3 << 5)
        Public Const USB_TYPE_STANDARD As Integer = (&H0 << 5)
        Public Const USB_TYPE_CLASS As Integer = (&H1 << 5)
        Public Const USB_TYPE_VENDOR As Integer = (&H2 << 5)
        Public Const USB_TYPE_RESERVED As Integer = (&H3 << 5)

        Public Const USB_RECIP_MASK As Integer = &H1F
        Public Const USB_RECIP_DEVICE As Integer = &H0
        Public Const USB_RECIP_INTERFACE As Integer = &H1
        Public Const USB_RECIP_ENDPOINT As Integer = &H2
        Public Const USB_RECIP_OTHER As Integer = &H3

        Public Const DeviceRequest As Integer = ((USB_DIR_IN Or USB_TYPE_STANDARD Or USB_RECIP_DEVICE) << 8) '//0x8000
        Public Const DeviceOutRequest As Integer = ((USB_DIR_OUT Or USB_TYPE_STANDARD Or USB_RECIP_DEVICE) << 8) '// 0x0000
        Public Const VendorDeviceRequest As Integer = ((USB_DIR_IN Or USB_TYPE_VENDOR Or USB_RECIP_DEVICE) << 8) '// 0xC000
        Public Const VendorDeviceOutRequest As Integer = ((USB_DIR_OUT Or USB_TYPE_VENDOR Or USB_RECIP_DEVICE) << 8) '//0x4000
        Public Const InterfaceRequest As Integer = ((USB_DIR_IN Or USB_TYPE_STANDARD Or USB_RECIP_INTERFACE) << 8) '// 0x8100
        Public Const InterfaceOutRequest As Integer = ((USB_DIR_OUT Or USB_TYPE_STANDARD Or USB_RECIP_INTERFACE) << 8) '//0x0100
        Public Const EndpointRequest As Integer = ((USB_DIR_IN Or USB_TYPE_STANDARD Or USB_RECIP_ENDPOINT) << 8) '//0x8200
        Public Const EndpointOutRequest As Integer = ((USB_DIR_OUT Or USB_TYPE_STANDARD Or USB_RECIP_ENDPOINT) << 8) '//0x0200

        Public Const ClassInterfaceRequest As Integer = ((USB_DIR_IN Or USB_TYPE_CLASS Or USB_RECIP_INTERFACE) << 8) '//0xA100
        Public Const ClassInterfaceOutRequest As Integer = ((USB_DIR_OUT Or USB_TYPE_CLASS Or USB_RECIP_INTERFACE) << 8) '//0x2100

        Public Const ClassEndpointRequest As Integer = ((USB_DIR_IN Or USB_TYPE_CLASS Or USB_RECIP_ENDPOINT) << 8) '//0xA200
        Public Const ClassEndpointOutRequest As Integer = ((USB_DIR_OUT Or USB_TYPE_CLASS Or USB_RECIP_ENDPOINT) << 8) '//0x2200

        Public Const USB_REQ_GET_STATUS As Integer = &H0
        Public Const USB_REQ_CLEAR_FEATURE As Integer = &H1
        Public Const USB_REQ_SET_FEATURE As Integer = &H3
        Public Const USB_REQ_SET_ADDRESS As Integer = &H5
        Public Const USB_REQ_GET_DESCRIPTOR As Integer = &H6
        Public Const USB_REQ_SET_DESCRIPTOR As Integer = &H7
        Public Const USB_REQ_GET_CONFIGURATION As Integer = &H8
        Public Const USB_REQ_SET_CONFIGURATION As Integer = &H9
        Public Const USB_REQ_GET_INTERFACE As Integer = &HA
        Public Const USB_REQ_SET_INTERFACE As Integer = &HB
        Public Const USB_REQ_SYNCH_FRAME As Integer = &HC

        Public Const USB_DEVICE_SELF_POWERED As Integer = 0
        Public Const USB_DEVICE_REMOTE_WAKEUP As Integer = 1

        Public Const USB_DT_DEVICE As Integer = &H1
        Public Const USB_DT_CONFIG As Integer = &H2
        Public Const USB_DT_STRING As Integer = &H3
        Public Const USB_DT_INTERFACE As Integer = &H4
        Public Const USB_DT_ENDPOINT As Integer = &H5
        Public Const USB_DT_CLASS As Integer = &H24
    End Module
End Namespace
