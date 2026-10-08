import { useEffect, useRef, useState } from 'react'
import { MessageCircle, Phone, PhoneOff, Send, X } from 'lucide-react'
import { Room, RoomEvent } from 'livekit-client'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { useScottyCallMutation, useScottyChatMutation } from '@/services/apiSlice'

const markdownClasses = [
  '[&_a]:text-primary [&_a]:underline',
  '[&_code]:rounded [&_code]:bg-background/70 [&_code]:px-1 [&_code]:py-0.5 [&_code]:text-xs',
  '[&_h1]:mb-1 [&_h1]:text-sm [&_h1]:font-semibold',
  '[&_h2]:mb-1 [&_h2]:text-sm [&_h2]:font-semibold',
  '[&_h3]:mb-1 [&_h3]:text-sm [&_h3]:font-semibold',
  '[&_h4]:mb-1 [&_h4]:text-sm [&_h4]:font-semibold',
  '[&_li]:ml-4',
  '[&_ol]:mb-2 [&_ol]:list-decimal',
  '[&_p]:mb-2 [&_p:last-child]:mb-0',
  '[&_pre]:mb-2 [&_pre]:overflow-x-auto [&_pre]:rounded [&_pre]:bg-background/70 [&_pre]:p-2 [&_pre]:text-xs',
  '[&_strong]:font-semibold',
  '[&_table]:w-full [&_table]:border-collapse',
  '[&_td]:border [&_td]:border-border [&_td]:px-1 [&_td]:py-0.5 [&_th]:border [&_th]:border-border [&_th]:px-1 [&_th]:py-0.5',
  '[&_ul]:mb-2 [&_ul]:list-disc',
].join(' ')

type ChatMessage = { role: 'user' | 'agent'; text: string }
type CallState = 'idle' | 'connecting' | 'connected'

const CONTINUITY_KEY = 'scotty_continuity_key'

function getContinuityKey(): string {
  const existing = localStorage.getItem(CONTINUITY_KEY)
  if (existing) return existing
  const key = crypto.randomUUID()
  localStorage.setItem(CONTINUITY_KEY, key)
  return key
}

function ScottyAssistant() {
  const [open, setOpen] = useState(false)
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [sending, setSending] = useState(false)
  const [callState, setCallState] = useState<CallState>('idle')
  const [error, setError] = useState<string | null>(null)
  const [scottyChat] = useScottyChatMutation()
  const [scottyCall] = useScottyCallMutation()
  const roomRef = useRef<Room | null>(null)
  const listRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight })
  }, [messages])

  async function sendMessage() {
    const text = input.trim()
    if (!text || sending) return
    setInput('')
    setSending(true)
    setError(null)
    setMessages((prev) => [...prev, { role: 'user', text }])
    try {
      const result = await scottyChat({ userPrompt: text, continuityKey: getContinuityKey() }).unwrap()
      setMessages((prev) => [...prev, { role: 'agent', text: result.output ?? '' }])
    } catch {
      setError('Something went wrong sending your message.')
      setMessages((prev) => [
        ...prev,
        { role: 'agent', text: 'Sorry, I could not reach the assistant right now.' },
      ])
    } finally {
      setSending(false)
    }
  }

  async function startCall() {
    setError(null)
    setCallState('connecting')
    try {
      const result = await scottyCall({ continuityKey: getContinuityKey() }).unwrap()
      if (!result.url || !result.token) throw new Error('missing credentials')
      const room = new Room()
      roomRef.current = room
      room
        .on(RoomEvent.TrackSubscribed, (track) => {
          if (track.kind === 'audio') {
            const element = new Audio()
            element.srcObject = new MediaStream([track.mediaStreamTrack])
            element.autoplay = true
            element.play().catch(() => {})
          }
        })
        .on(RoomEvent.Disconnected, () => {
          setCallState('idle')
          roomRef.current = null
        })
      await room.connect(result.url, result.token)
      await room.localParticipant.setMicrophoneEnabled(true)
      setCallState('connected')
    } catch {
      setCallState('idle')
      setError('Could not start the voice call.')
    }
  }

  function endCall() {
    roomRef.current?.disconnect()
    roomRef.current = null
    setCallState('idle')
  }

  return (
    <>
      <button
        type="button"
        aria-label="Open assistant"
        onClick={() => setOpen((v) => !v)}
        className="fixed bottom-6 right-6 z-50 flex h-14 w-14 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg transition-transform hover:scale-105"
      >
        {open ? <X /> : <MessageCircle />}
      </button>

      {open && (
        <div className="fixed bottom-24 right-6 z-50 flex h-[28rem] w-96 flex-col overflow-hidden rounded-xl border bg-background shadow-2xl">
          <div className="flex items-center justify-between border-b px-4 py-3">
            <div>
              <p className="text-sm font-semibold">AITS Assistant</p>
              <p className="text-xs text-muted-foreground">
                {callState === 'connected'
                  ? 'In voice call'
                  : callState === 'connecting'
                    ? 'Connecting call...'
                    : 'Ask me anything'}
              </p>
            </div>
            <div className="flex items-center gap-2">
              {callState === 'idle' ? (
                <button
                  type="button"
                  aria-label="Start voice call"
                  onClick={startCall}
                  className="flex h-9 w-9 items-center justify-center rounded-full bg-green-600 text-white transition-colors hover:bg-green-700"
                >
                  <Phone className="h-4 w-4" />
                </button>
              ) : (
                <button
                  type="button"
                  aria-label="End voice call"
                  onClick={endCall}
                  className="flex h-9 w-9 items-center justify-center rounded-full bg-red-600 text-white transition-colors hover:bg-red-700"
                >
                  <PhoneOff className="h-4 w-4" />
                </button>
              )}
            </div>
          </div>

          {error && <p className="border-b px-4 py-2 text-xs text-destructive">{error}</p>}

          <div ref={listRef} className="flex-1 space-y-3 overflow-y-auto px-4 py-3">
            {messages.length === 0 && (
              <p className="text-center text-sm text-muted-foreground">
                Hello! I&apos;m here to help with AI Talent Sourcing. What can I do for you?
              </p>
            )}
            {messages.map((message, i) =>
              message.role === 'user' ? (
                <div
                  key={i}
                  className="ml-auto max-w-[80%] whitespace-pre-wrap rounded-lg bg-primary px-3 py-2 text-sm text-primary-foreground"
                >
                  {message.text}
                </div>
              ) : (
                <div
                  key={i}
                  className={`max-w-[80%] rounded-lg bg-muted px-3 py-2 text-sm ${markdownClasses}`}
                >
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>{message.text}</ReactMarkdown>
                </div>
              ),
            )}
            {sending && <p className="text-sm text-muted-foreground">Agent is typing...</p>}
          </div>

          <div className="flex items-center gap-2 border-t p-3">
            <input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') sendMessage()
              }}
              placeholder="Type a message..."
              className="h-9 flex-1 rounded-md border bg-transparent px-3 text-sm outline-none focus:border-ring"
            />
            <button
              type="button"
              aria-label="Send message"
              onClick={sendMessage}
              disabled={sending || !input.trim()}
              className="flex h-9 w-9 items-center justify-center rounded-md bg-primary text-primary-foreground transition-opacity disabled:opacity-40"
            >
              <Send className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}
    </>
  )
}

export default ScottyAssistant