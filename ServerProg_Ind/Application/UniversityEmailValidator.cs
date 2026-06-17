namespace ServerProg_Ind.Application;

internal static class UniversityEmailValidator
{
    public static bool IsAllowed(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var candidate = email.Trim();
        if (candidate.Length > 254)
        {
            return false;
        }

        var atIndex = candidate.IndexOf('@');
        if (atIndex <= 0 || atIndex != candidate.LastIndexOf('@') || atIndex == candidate.Length - 1)
        {
            return false;
        }

        var localPart = candidate[..atIndex];
        var domain = candidate[(atIndex + 1)..];

        return IsValidLocalPart(localPart) && IsValidSfeduDomain(domain);
    }

    private static bool IsValidLocalPart(string localPart)
    {
        if (localPart.Length is 0 or > 64)
        {
            return false;
        }

        if (localPart.StartsWith('.') || localPart.EndsWith('.') || localPart.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        return localPart.All(IsAllowedLocalCharacter);
    }

    private static bool IsValidSfeduDomain(string domain)
    {
        if (domain.Length is 0 or > 253)
        {
            return false;
        }

        if (domain.StartsWith('.') || domain.EndsWith('.') || domain.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var labels = domain.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (labels.Length < 2)
        {
            return false;
        }

        if (!labels.Any(label => string.Equals(label, "sfedu", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (labels.Any(label => !IsValidDomainLabel(label)))
        {
            return false;
        }

        var topLevelDomain = labels[^1];
        return topLevelDomain.Length >= 2 && topLevelDomain.All(IsAsciiLetter);
    }

    private static bool IsValidDomainLabel(string label)
    {
        if (label.Length is 0 or > 63)
        {
            return false;
        }

        if (label.StartsWith('-') || label.EndsWith('-'))
        {
            return false;
        }

        return label.All(IsAsciiLetterOrDigitOrHyphen);
    }

    private static bool IsAllowedLocalCharacter(char character)
    {
        return IsAsciiLetterOrDigit(character) || character is '.' or '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-' or '/' or '=' or '?' or '^' or '_' or '`' or '{' or '|' or '}' or '~';
    }

    private static bool IsAsciiLetterOrDigitOrHyphen(char character)
    {
        return IsAsciiLetterOrDigit(character) || character == '-';
    }

    private static bool IsAsciiLetterOrDigit(char character)
    {
        return IsAsciiLetter(character) || character is >= '0' and <= '9';
    }

    private static bool IsAsciiLetter(char character)
    {
        return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }
}
