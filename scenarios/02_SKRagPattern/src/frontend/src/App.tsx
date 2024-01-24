//import './App.css'

import { For, createEffect, createSignal } from "solid-js"
import SolidMarkdown from "solid-markdown"
import { IMessage, IQuery, ISettings } from "./components/interfaces"
import { makePersisted } from "@solid-primitives/storage"
import { Puff } from "solid-spinner"

const QUERY_ENDPOINT = import.meta.env.VITE_URL_QUERY
const COLLECTIONS_ENDPOINT = import.meta.env.VITE_URL_COLLECTIONS

const DefaultSettings: ISettings = {
  max_tokens: "2000",
  temperature: "0.3",
  limit: "3",
  minRelevance: "0.77"
}

function App() {

  const [settings, setSettings] = makePersisted(createSignal(DefaultSettings))
  const [query, setQuery] = createSignal("")
  const [conversation, setConversation] = makePersisted(createSignal<IMessage[]>([]))
  const [collections, setCollections] = createSignal<string[]>([])
  const [selectedCollection, setSelectedCollection] = createSignal<string>("")
  const [searching, setSearching] = createSignal(false)

  createEffect(async () => {
    // Load collections
    const res = await fetch(COLLECTIONS_ENDPOINT)
    const data = await res.json()
    setCollections(data)
    if (data.length > 0)
      setSelectedCollection(data[0])
  })

  const SendQuery = async () => {
    setSearching(true)
    try {
      // TODO: Put a spinner
      const payload: IQuery = {
        collection: selectedCollection(),
        query: query(),
        maxTokens: parseInt(settings().max_tokens),
        temperature: parseFloat(settings().temperature),
        limit: parseInt(settings().limit),
        minRelevanceScore: parseFloat(settings().minRelevance)
      }
      const res = await fetch(QUERY_ENDPOINT, {
        method: "POST",
        headers: { "Content-Type": "application/json", "Accept": "application/json" },
        body: JSON.stringify(payload)
      })
      const data = await res.json()
      console.info(JSON.stringify(data, null, 2))
      setConversation([...conversation(), {
        query: query(),
        collection: data.collection,
        text: data.text,
        usage: data.usage,
        citations: data.citations,
        ts: (new Date()).toISOString()
      }])
      setQuery("")
    }
    catch (err) {
      console.error(err)
    }
    finally {
      setSearching(false)
    }
  }

  const Reset = () => {
    // Resets settings and conversation
    setSettings(DefaultSettings)
    setConversation([])
  }

  return (
    <>
      <nav class='p-2 bg-blue-950 text-white font-semibold text-lg'>
        <h1>Search your documents</h1>
      </nav>
      <nav class='p-2 bg-blue-900 text-white font-semibold text-sm flex flex-row flex-wrap space-x-2 items-center'>
        <div class='space-x-2'>
          <label>Max Tokens:</label>
          <input type="text" class='px-1 w-16 text-black'
            value={settings().max_tokens}
            onInput={e => setSettings({ ...settings(), max_tokens: e.currentTarget.value })}
          />
        </div>
        <div class='space-x-2'>
          <label>Temperature:</label>
          <input type="text" class='px-1 w-16 text-black'
            value={settings().temperature}
            onInput={e => setSettings({ ...settings(), temperature: e.currentTarget.value })}
          />
        </div>
        <div class='space-x-2'>
          <label>Limit:</label>
          <input type="text" class='px-1 w-16 text-black'
            value={settings().limit}
            onInput={e => setSettings({ ...settings(), limit: e.currentTarget.value })}
          />
        </div>
        <div class='space-x-2'>
          <label>Relevance:</label>
          <input type="text" class='px-1 w-16 text-black'
            value={settings().minRelevance}
            onInput={e => setSettings({ ...settings(), minRelevance: e.currentTarget.value })}
          />
        </div>
        <button
          class="bg-red-700 text-white hover:bg-red-600 rounded p-1"
          onClick={Reset}
          disabled={searching()}
        >Clear & Reset</button>
        <div class={(searching() ? 'visible' : 'hidden')}>
          <Puff color="white" width={20} height={20} />
        </div>
      </nav>
      <main class="container mx-auto">
        <div class="p-3 flex flex-col w-full space-y-2">
          <div>
            <label class="text-sm font-semibold uppercase">Select an area:</label>
          </div>
          <div class="flex flex-row flex-wrap space-x-2">
            <For each={collections()}>
              {collection => (
                <div class="bg-purple-700 p-2">
                  <input type="radio" class="mr-1" value={collection}
                    checked={selectedCollection() === collection}
                    onClick={() => setSelectedCollection(collection)}
                  />
                  <label class="mr-2 text-white">{collection}</label>
                </div>
              )}
            </For>
          </div>
          <div class="flex flex-row">
            <textarea class="p-2 border w-[calc(100%-70px)] mr-[5px]" rows={5}
              placeholder="What is your question?"
              value={query()}
              onInput={e => setQuery(e.currentTarget.value)}
              onKeyPress={e => { if (e.key === "Enter") SendQuery() }}
            />
            <button class="p-2 bg-blue-700 hover:bg-blue-600 text-white text-sm font-semibold w-[75px] ml-auto"
              onClick={SendQuery} disabled={searching()}
            >Search</button>
          </div>
          {/* TODO: Iterate over the array */}
          <For each={conversation()}>
            {message => (
              <>
                <div class="p-2 bg-blue-200 w-[90%] rounded-md">{message.query}</div>
                <div class="p-2 bg-blue-300 w-[90%] rounded-md ml-auto">
                  <div class="p-2 mt-1 mb-1 bg-purple-700 text-white font-semibold">{(new Date(message.ts)).toLocaleString()} - {message.collection}</div>
                  <SolidMarkdown children={message.text} />
                  <hr />
                  <div class="flex flex-row flex-wrap justify-center p-2">
                    <For each={message.citations}>
                      {citation => (
                        <div class="p-2 m-1 bg-blue-500 mr-2 font-semibold text-white">
                          <a href={citation.description} class="hover:underline">{citation.fileName}</a>
                        </div>
                      )}
                    </For>
                  </div>
                </div>
              </>
            )}
          </For>
        </div>
      </main>
    </>
  )
}

export default App
