import { useState } from 'react'
import { Plus, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

// Comma-separated values shown as removable tags. Add a value with Enter or the Add button;
// click x on a tag to remove that value.
export function TagInput({
  value,
  onChange,
  placeholder,
}: {
  value: string
  onChange: (value: string) => void
  placeholder?: string
}) {
  const [text, setText] = useState('')

  const items = (value ?? '')
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean)

  const add = () => {
    const parts = text
      .split(',')
      .map((part) => part.trim())
      .filter(Boolean)

    if (parts.length === 0) {
      return
    }

    const next = [...items]
    for (const part of parts) {
      if (!next.some((item) => item.toLowerCase() === part.toLowerCase())) {
        next.push(part)
      }
    }

    onChange(next.join(', '))
    setText('')
  }

  const remove = (item: string) => {
    onChange(items.filter((entry) => entry !== item).join(', '))
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-1.5">
        {items.length === 0 ? (
          <span className="text-xs text-muted-foreground">None added yet.</span>
        ) : (
          items.map((item) => (
            <span
              key={item}
              className="inline-flex items-center gap-1 rounded-full border bg-muted/40 px-2 py-0.5 text-xs"
            >
              {item}
              <button
                type="button"
                onClick={() => remove(item)}
                aria-label={`Remove ${item}`}
                className="text-muted-foreground hover:text-foreground"
              >
                <X className="h-3 w-3" />
              </button>
            </span>
          ))
        )}
      </div>
      <div className="flex gap-2">
        <Input
          value={text}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault()
              add()
            }
          }}
          placeholder={placeholder}
        />
        <Button type="button" variant="outline" onClick={add} disabled={!text.trim()}>
          <Plus />
          Add
        </Button>
      </div>
    </div>
  )
}