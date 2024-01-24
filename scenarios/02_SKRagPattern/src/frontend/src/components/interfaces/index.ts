export interface ISettings {
    max_tokens: string,
    temperature: string,
    limit: string,
    minRelevance: string
}

export interface IMessage {
    query: string
    collection: string
    text: string
    usage: {
        completionTokens: number
        promptTokens: number
        totalTokens: number
    }
    citations: {
        collection: string
        fileName: string
        description: string
        additionalMetadata: string
    }[]
    ts: string
}

export interface IQuery {
    collection: string
    query: string
    maxTokens: number
    temperature: number
    limit: number
    minRelevanceScore: number
}

