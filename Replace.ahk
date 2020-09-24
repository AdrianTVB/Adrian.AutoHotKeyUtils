; Sourced from https://superuser.com/questions/595456/is-there-a-paste-replace-shortcut

^F1:: ;work with clipboard: swap clipboard and selection
    ClipboardOld := ClipboardAll
    Clipboard =     ;empty the clipboard so ClipWait statement can wait until it is filled
    Send ^c
    ClipWait, , 1
    ClipboardNew := ClipboardAll
    Clipboard := ClipboardOld
    ClipboardOld =  ;free the memory in case the clipboard was very large
    Sleep 250   ;wait until Clipboard is updated
    Send ^v
    Sleep 250   ;wait until Paste operation is completed
    Clipboard := ClipboardNew
    ClipboardNew =  ;free the memory in case the clipboard was very large
    Return