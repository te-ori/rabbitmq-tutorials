using System;

public static class Helpers
{
    public static byte[] ToMessage(this string s){
        return System.Text.Encoding.UTF8.GetBytes(s);
    }

    public static string ToMessageString(this byte[] b){
        return System.Text.Encoding.UTF8.GetString(b);
    }

    public static string ToMessageString(this ReadOnlyMemory<byte> b){
        return System.Text.Encoding.UTF8.GetString(b.ToArray());
    }

    public static byte[] ToMessageAsString(this int i){
        return i.ToString().ToMessage();
    }
}
