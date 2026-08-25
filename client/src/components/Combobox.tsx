import { useEffect, useRef, useState } from 'react'
import { Loader2 } from 'lucide-react'
import { useLazyAutocompleteQuery } from '@/services/apiSlice'
import { cn } from '@/lib/utils'

export function SuggestionInput({
  field,
  value,
  onChange,
  placeholder,
  className,
}: {
  field: 'countries' | 'skill_names'
  value?: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
}) {
  const [trigger, { isFetching }] = useLazyAutocompleteQuery()
  const [text, setText] = useState(value ?? '')
  const [suggestions, setSuggestions] = useState<string[]>([])
  const [failed, setFailed] = useState(false)
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const external = value ?? ''
    setText((current) => (current === external ? current : external))
  }, [value])

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  useEffect(() => {
    if (!open || text.trim().length < 2) {
      setSuggestions([])
      setFailed(false)
      return
    }
    const timer = setTimeout(async () => {
      try {
        const result = await trigger({ field, inputText: text.trim() }).unwrap()
        setSuggestions(result.map((s) => s.content))
        setFailed(false)
      } catch {
        setSuggestions([])
        setFailed(true)
      }
    }, 300)
    return () => clearTimeout(timer)
  }, [text, open, field, trigger])

  return (
    <div ref={containerRef} className={cn('relative', className)}>
      <div className="relative">
        <input
          type="text"
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 pr-8 text-sm shadow-xs placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          value={text}
          placeholder={placeholder}
          onChange={(e) => {
            setText(e.target.value)
            setOpen(true)
            onChange(e.target.value)
          }}
          onFocus={() => setOpen(true)}
        />
        {isFetching && (
          <Loader2 className="absolute right-2.5 top-2.5 h-4 w-4 animate-spin text-muted-foreground" />
        )}
      </div>
      {open && text.trim().length >= 2 && !isFetching && failed && (
        <div className="absolute z-20 mt-1 w-full rounded-md border border-destructive/30 bg-popover px-3 py-2 text-sm text-destructive shadow-md">
          Can't reach the server - is the API running?
        </div>
      )}
      {open && text.trim().length >= 2 && !isFetching && !failed && suggestions.length === 0 && (
        <div className="absolute z-20 mt-1 w-full rounded-md border bg-popover px-3 py-2 text-sm text-muted-foreground shadow-md">
          No matches found
        </div>
      )}
      {open && suggestions.length > 0 && (
        <ul className="absolute z-20 mt-1 max-h-56 w-full overflow-auto rounded-md border bg-popover py-1 text-sm shadow-md">
          {suggestions.map((s) => (
            <li key={s}>
              <button
                type="button"
                className="w-full px-3 py-1.5 text-left hover:bg-accent hover:text-accent-foreground"
                onClick={() => {
                  setText(s)
                  onChange(s)
                  setOpen(false)
                }}
              >
                {s}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

