public class TextChunker
{

    public static string CleanUp(string str)
    {
        // Trim
        str = str.Trim();
        // Remove extra newlines
        str = Regex.Replace(str, @"\n{3,}", "\n\n");
        // Remove carriage returns
        str = Regex.Replace(str, @"\r", "");
        // Remove extra spaces
        str = Regex.Replace(str, @" {3,}", " ");
        // Remove tabs
        str = Regex.Replace(str, @"\t{1,}", "");

        return str;
    }

    public static bool IsTeamsTranscriptDetected(string text)
    {
        //TODO: Make this smarter
        if (text.Contains("-->") && text.Contains(':'))
            return true;
        return false;
    }

    public static List<ChunkInfo> ChunkText(string fullText, int chunkSize = 500)
    {
        if (chunkSize <= 0)
        {
            chunkSize = 500;
        }

        if (IsTeamsTranscriptDetected(fullText))
            fullText = TeamsFilter(fullText);

        Console.WriteLine("Chunking text into chunks of max size: " + chunkSize);

        // Split into paragraphs
        string[] chunks = Regex.Split(CleanUp(fullText), @"\n\n");
        List<ChunkInfo> sections = new List<ChunkInfo>();

        // Process each paragraph
        string section = "";
        int idx = 0;
        foreach (string paragraph in chunks)
        {
            section += paragraph + "\n\n";
            int words = section.Split(' ').Length;
            // If adding paragraph exceeds max size, add to sections
            if (words > chunkSize)
            {
                var sectionInfo = new ChunkInfo
                {
                    Index = idx,
                    Content = section,
                    Words = words,
                    Characters = section.Length,
                    Tokens = 0
                };
                sections.Add(sectionInfo);
                // Start a new section
                section = "";
                idx++;
            }
        }

        // If there was a section remaining, add it
        if (section != "")
        {
            ChunkInfo sectionInfo = new ChunkInfo
            {
                Index = idx,
                Content = section,
                Words = section.Split(' ').Length,
                Characters = section.Length,
                Tokens = 0
            };
            sections.Add(sectionInfo);
        }

        Console.WriteLine("Chunked text into " + sections.Count + " chunk(s)");

        return sections;
    }

    public static string TeamsFilter(string text)
    {
        string[] lines = text.Split("\n");
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Contains("-->"))
            {
                sb.Append("\n\n");
                sb.Append(lines[i + 1]);
                sb.Append(": ");
                i++;
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }
}