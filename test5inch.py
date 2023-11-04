from math import ceil
import serial
import cv2
import atexit

class CMD():
    Get_Device = '01ef6900000001000000c5d3'
    Update_IMG = 'ccef690000'
    Stop_Video = '79ef6900000001'
    Display_Full_IMAGE = 'c8ef69001770'
    Query_Render_Status = 'cfef6900000001'

class Unknown():
    PreImgCMD = '2c'
    Media_Stop = '96ef6900000001'
    PostImgCMD = '86ef6900000001'
    OnExit = '87ef6900000001'
  
def OnExit():
    SendMSG(Unknown.OnExit)
    lcd_serial.close()


def ReadReply():
    response = lcd_serial.read(1024).decode('utf-8')
    print(response)

def SendMSG(MSG, PadValue = '00'):
    if type(MSG) is str: MSG = bytearray.fromhex(MSG)

    MsgSize = len(MSG)
    if not (MsgSize / 250).is_integer(): MSG += bytes.fromhex(PadValue) * ((250 * ceil(MsgSize / 250)) - MsgSize)
    # print("sending:")
    # print(MSG)
    lcd_serial.write(MSG)
    return
    
    #I didn't notice any speed difference in splitting the messages, but their app had random splits in their messages ...
    MsgLimit = 111000
    MSG = [MSG[i:i + MsgLimit] for i in range(0, len(MSG), MsgLimit)]    
    for part in MSG: lcd_serial.write(part)


def GenerateFullImage(Path):
    image = cv2.imread(Path, cv2.IMREAD_UNCHANGED)        
    if image.shape[2] < 4: image = cv2.cvtColor(image, cv2.COLOR_BGR2BGRA)

    image = bytearray(image)
    image = b'\x00'.join(image[i:i + 249] for i in range(0, len(image), 249))
    return image

def GenerateUpdateImage(Path, x, y):
    image = cv2.imread(Path, cv2.IMREAD_UNCHANGED)
    height, width, channels = image.shape 
    MSG = ''
    for h in range(height): 
        MSG += f'{((x + h) * 800) + y:06x}'  + f'{width:04x}'       
        for w in range(width): MSG += f'{image[h][w][0]:02x}' + f'{image[h][w][1]:02x}' + f'{image[h][w][2]:02x}'

    UPD_Size = f'{int((len(MSG) / 2) + 2):04x}' #The +2 is for the "ef69" that will be added later
    
    if len(MSG) > 500: MSG = '00'.join(MSG[i:i + 498] for i in range(0, len(MSG), 498))
    MSG += 'ef69'
    print(UPD_Size)
    return MSG, UPD_Size


lcd_serial = serial.Serial("COM5", 115200, timeout=1, rtscts=1)
atexit.register(OnExit)

SendMSG(CMD.Get_Device)                     #Skippable 
ReadReply()
SendMSG(CMD.Stop_Video)                     #Skippable if there is no video playing now
SendMSG(Unknown.Media_Stop)                 #Skippable, might be for album playback
ReadReply()                                 #The reply should be "media_stop"
SendMSG(Unknown.PreImgCMD, '2c')            #Skippable, the app pads it using "2c" instead of 00

SendMSG(CMD.Display_Full_IMAGE)
image = GenerateFullImage(r'test1.png')
SendMSG(image)
ReadReply()                                 #The reply should be "full_png_sucess"
SendMSG(Unknown.PostImgCMD)                 #Skippable 

SendMSG(CMD.Query_Render_Status) 
ReadReply()                                 #The reply should containts (needReSend:0) to confirm all message are read/deliverd in order

MSG, UPD_Size = GenerateUpdateImage(r'test2-crop.png',100,50)
SendMSG(CMD.Update_IMG + UPD_Size)
SendMSG(MSG)

SendMSG(CMD.Query_Render_Status) 
ReadReply()                                 #The reply should containts (needReSend:0) to confirm all message are read/deliverd in order