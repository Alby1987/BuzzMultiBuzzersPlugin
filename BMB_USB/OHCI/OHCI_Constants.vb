Namespace OHCI

    Module OHCI_Constants

        Public Const OHCI_MAX_PORTS As Integer = 2

        '/* Bitfields for the first word of an Endpoint Descriptor. */
        Public Const OHCI_ED_FA_SHIFT As UInt32 = 0 '//device address
        Public Const OHCI_ED_FA_MASK As UInt32 = (&H7F << OHCI_ED_FA_SHIFT)
        Public Const OHCI_ED_EN_SHIFT As UInt32 = 7 '//endpoint number
        Public Const OHCI_ED_EN_MASK As UInt32 = (&HF << OHCI_ED_EN_SHIFT)
        Public Const OHCI_ED_D_SHIFT As UInt32 = 11 '//direction
        Public Const OHCI_ED_D_MASK As UInt32 = (3 << OHCI_ED_D_SHIFT)
        Public Const OHCI_ED_S As UInt32 = (1 << 13) '//speed 0 - full, 1 - low
        Public Const OHCI_ED_K As UInt32 = (1 << 14) '//skip ED if 1
        Public Const OHCI_ED_MPS_SHIFT As UInt32 = 16
        Public Const OHCI_ED_MPS_MASK As UInt32 = (&H7FF << OHCI_ED_MPS_SHIFT)
        Public Const OHCI_ED_F As UInt32 = (1 << 15) '//format 0 - inter, bulk or setup, 1 - isoch

        '/* Flags in the head field of an Endpoint Descriptor. */
        Public Const OHCI_ED_H As UInt32 = 1 '//halted
        Public Const OHCI_ED_C As UInt32 = 2

        '/* Bitfields for the first word of a Transfer Descriptor. */
        Public Const OHCI_TD_R As UInt32 = (1 << 18)
        Public Const OHCI_TD_DP_SHIFT As UInt32 = 19
        Public Const OHCI_TD_DP_MASK As UInt32 = (3 << OHCI_TD_DP_SHIFT)
        Public Const OHCI_TD_DI_SHIFT As UInt32 = 21
        Public Const OHCI_TD_DI_MASK As UInt32 = (7 << OHCI_TD_DI_SHIFT)
        Public Const OHCI_TD_T0 As UInt32 = (1 << 24)
        Public Const OHCI_TD_T1 As UInt32 = (1 << 24)
        Public Const OHCI_TD_EC_SHIFT As UInt32 = 26
        Public Const OHCI_TD_EC_MASK As UInt32 = (3 << OHCI_TD_EC_SHIFT)
        Public Const OHCI_TD_CC_SHIFT As UInt32 = 28
        Public Const OHCI_TD_CC_MASK As UInt32 = (&HFUI << OHCI_TD_CC_SHIFT)

        '/* Bitfields for the first word of an Isochronous Transfer Descriptor. */
        '/* CC & DI - same as in the General Transfer Descriptor */
        Public Const OHCI_TD_SF_SHIFT As UInt32 = 0
        Public Const OHCI_TD_SF_MASK As UInt32 = (&HFFFF << OHCI_TD_SF_SHIFT)
        Public Const OHCI_TD_FC_SHIFT As UInt32 = 24
        Public Const OHCI_TD_FC_MASK As UInt32 = (7 << OHCI_TD_FC_SHIFT)

        '/* Isochronous Transfer Descriptor - Offset / PacketStatusWord */
        Public Const OHCI_TD_PSW_CC_SHIFT As UInt32 = 12
        Public Const OHCI_TD_PSW_CC_MASK As UInt32 = (&HF << OHCI_TD_PSW_CC_SHIFT)
        Public Const OHCI_TD_PSW_SIZE_SHIFT As UInt32 = 0
        Public Const OHCI_TD_PSW_SIZE_MASK As UInt32 = (&HFFF << OHCI_TD_PSW_SIZE_SHIFT)

        Public Const OHCI_PAGE_MASK As UInt32 = &HFFFFF000UI
        Public Const OHCI_OFFSET_MASK As UInt32 = &HFFF

        Public Const OHCI_DPTR_MASK As UInt32 = &HFFFFFFF0UI

        '/* OHCI Local stuff */
        Public Const OHCI_CTL_CBSR As UInt32 = ((1 << 0) Or (1 << 1))
        Public Const OHCI_CTL_PLE As UInt32 = (1 << 2)
        Public Const OHCI_CTL_IE As UInt32 = (1 << 3)
        Public Const OHCI_CTL_CLE As UInt32 = (1 << 4)
        Public Const OHCI_CTL_BLE As UInt32 = (1 << 5)
        Public Const OHCI_CTL_HCFS As UInt32 = ((1 << 6) Or (1 << 7))
        Public Const OHCI_USB_RESET As UInt32 = &H0
        Public Const OHCI_USB_RESUME As UInt32 = &H40
        Public Const OHCI_USB_OPERATIONAL As UInt32 = &H80
        Public Const OHCI_USB_SUSPEND As UInt32 = &HC0
        Public Const OHCI_CTL_IR As UInt32 = (1 << 8)
        Public Const OHCI_CTL_RWC As UInt32 = (1 << 9)
        Public Const OHCI_CTL_RWE As UInt32 = (1 << 10)

        Public Const OHCI_STATUS_HCR As UInt32 = (1 << 0)
        Public Const OHCI_STATUS_CLF As UInt32 = (1 << 1)
        Public Const OHCI_STATUS_BLF As UInt32 = (1 << 2)
        Public Const OHCI_STATUS_OCR As UInt32 = (1 << 3)
        Public Const OHCI_STATUS_SOC As UInt32 = ((1 << 6) Or (1 << 7))

        Public Const OHCI_INTR_SO As UInt32 = (1 << 0) '/* Scheduling overrun */
        Public Const OHCI_INTR_WD As UInt32 = (1 << 1) '/* HcDoneHead writeback */
        Public Const OHCI_INTR_SF As UInt32 = (1 << 2) '/* Start of frame */
        Public Const OHCI_INTR_RD As UInt32 = (1 << 3) '/* Resume detect */
        Public Const OHCI_INTR_UE As UInt32 = (1 << 4) '/* Unrecoverable error */
        Public Const OHCI_INTR_FNO As UInt32 = (1 << 5) '/* Frame number overflow */
        Public Const OHCI_INTR_RHSC As UInt32 = (1 << 6) '/* Root hub status change */
        Public Const OHCI_INTR_OC As UInt32 = (1 << 30) '/* Ownership change */
        Public Const OHCI_INTR_MIE As UInt32 = (1UI << 31UI) '/* Master Interrupt Enable */

        '#define OHCI_HCCA_SIZE 0x100
        Public Const OHCI_HCCA_MASK As UInt32 = &HFFFFFF00UI

        Public Const OHCI_EDPTR_MASK As UInt32 = &HFFFFFFF0UI

        Public Const OHCI_FMI_FI As UInt32 = &H3FFF
        Public Const OHCI_FMI_FSMPS As UInt32 = &HFFFF0000UI
        Public Const OHCI_FMI_FIT As UInt32 = &H80000000UI

        Public Const OHCI_FR_RT As UInt32 = (1UI << 31)

        Public Const OHCI_LS_THRESH As UInt32 = &H628

        Public Const OHCI_RHA_RW_MASK As UInt32 = &H0 '/* Mask of supported features. */
        Public Const OHCI_RHA_PSM As UInt32 = (1 << 8)
        Public Const OHCI_RHA_NPS As UInt32 = (1 << 9)
        Public Const OHCI_RHA_DT As UInt32 = (1 << 10)
        Public Const OHCI_RHA_OCPM As UInt32 = (1 << 11)
        Public Const OHCI_RHA_NOCP As UInt32 = (1 << 12)
        Public Const OHCI_RHA_POTPGT_MASK As UInt32 = &HFF000000UI

        Public Const OHCI_RHS_LPS As UInt32 = (1 << 0)
        Public Const OHCI_RHS_OCI As UInt32 = (1 << 1)
        Public Const OHCI_RHS_DRWE As UInt32 = (1 << 15)
        Public Const OHCI_RHS_LPSC As UInt32 = (1 << 16)
        Public Const OHCI_RHS_OCIC As UInt32 = (1 << 17)
        Public Const OHCI_RHS_CRWE As UInt32 = (1UI << 31UI)

        Public Const OHCI_PORT_CCS As UInt32 = (1 << 0)
        Public Const OHCI_PORT_PES As UInt32 = (1 << 1)
        Public Const OHCI_PORT_PSS As UInt32 = (1 << 2)
        Public Const OHCI_PORT_POCI As UInt32 = (1 << 3)
        Public Const OHCI_PORT_PRS As UInt32 = (1 << 4)
        Public Const OHCI_PORT_PPS As UInt32 = (1 << 8)
        Public Const OHCI_PORT_LSDA As UInt32 = (1 << 9)
        Public Const OHCI_PORT_CSC As UInt32 = (1 << 16)
        Public Const OHCI_PORT_PESC As UInt32 = (1 << 17)
        Public Const OHCI_PORT_PSSC As UInt32 = (1 << 18)
        Public Const OHCI_PORT_OCIC As UInt32 = (1 << 19)
        Public Const OHCI_PORT_PRSC As UInt32 = (1 << 20)
        Public Const OHCI_PORT_WTC As UInt32 = (OHCI_PORT_CSC Or OHCI_PORT_PESC Or OHCI_PORT_PSSC Or OHCI_PORT_OCIC Or OHCI_PORT_PRSC)

        Public Const OHCI_TD_DIR_SETUP As Integer = &H0
        Public Const OHCI_TD_DIR_OUT As Integer = &H1
        Public Const OHCI_TD_DIR_IN As Integer = &H2
        Public Const OHCI_TD_DIR_RESERVED As Integer = &H3

        Public Const OHCI_CC_NOERROR As UInt32 = &H0
        Public Const OHCI_CC_CRC As UInt32 = &H1
        Public Const OHCI_CC_BITSTUFFING As UInt32 = &H2
        Public Const OHCI_CC_DATATOGGLEMISMATCH As UInt32 = &H3
        Public Const OHCI_CC_STALL As UInt32 = &H4
        Public Const OHCI_CC_DEVICENOTRESPONDING As UInt32 = &H5
        Public Const OHCI_CC_PIDCHECKFAILURE As UInt32 = &H6
        Public Const OHCI_CC_UNDEXPETEDPID As UInt32 = &H7 '// the what?
        Public Const OHCI_CC_DATAOVERRUN As UInt32 = &H8
        Public Const OHCI_CC_DATAUNDERRUN As UInt32 = &H9
        Public Const OHCI_CC_BUFFEROVERRUN As UInt32 = &HC
        Public Const OHCI_CC_BUFFERUNDERRUN As UInt32 = &HD

        Public Const OHCI_HRESET_FSBIR As UInt32 = (1 << 0)
    End Module
End Namespace
