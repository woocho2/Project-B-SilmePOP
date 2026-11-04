using TMPro;

// Login_LoginPanel·Login_SignUpPanel이 같이 쓰는 입력칸 도우미
public static class Login_AuthInput
{
    // ●●●● 로 가림
    public static void HidePassword(TMP_InputField field)
    {
        if (field == null) return;
        field.contentType = TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
    }

    public static string Text(TMP_InputField field)
    {
        return field != null ? field.text : string.Empty;
    }
}
