const string SummaryTemplate = @"Write a summary of the transcript, list the Azure services discussed, list the other technologies discussed, and list the action items.

Desired format:

Summary:

Azure services:

-||-


Other technologies:

-||-


Action items:

-||-


Text: """"""
{context}
""""""
";

const string FinalSummaryTemplate = @"These are transcripts summaries that belongs to the same call. Collate the summaries into one summary, collate the list of Azure Services into one list, collate the list of other technologies into one list, and collate the list of action items into one list.

Desired format:

Summary:

Azure services:

-||-


Other technologies:

-||-


Action items:

-||-


Text: """"""
{context}
""""""

";

