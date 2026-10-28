import { useEffect, useRef, useState, type ReactNode } from 'react'
import { Bold, Braces, Italic, Link2, List } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface EditorAttribute {
  label: string
  token: string
}

export interface EditorAttributeGroup {
  label: string
  items: EditorAttribute[]
}

interface RichTextEditorProps {
  value: string
  onChange: (html: string) => void
  placeholder?: string
  attributes?: EditorAttributeGroup[]
  showLink?: boolean
  className?: string
}

export function RichTextEditor({
  value,
  onChange,
  placeholder,
  attributes = [],
  showLink = true,
  className,
}: RichTextEditorProps) {
  const editorRef = useRef<HTMLDivElement>(null)
  const [menuOpen, setMenuOpen] = useState(false)

  useEffect(() => {
    const el = editorRef.current
    if (el && el.innerHTML !== (value ?? '')) {
      el.innerHTML = value ?? ''
    }
  }, [value])

  const emit = () => {
    if (editorRef.current) onChange(editorRef.current.innerHTML)
  }

  const exec = (command: string, arg?: string) => {
    editorRef.current?.focus()
    document.execCommand(command, false, arg)
    emit()
  }

  const insertToken = (token: string) => {
    editorRef.current?.focus()
    document.execCommand('insertText', false, token)
    emit()
    setMenuOpen(false)
  }

  const addLink = () => {
    const url = window.prompt('Link URL', 'https://')
    if (url) exec('createLink', url)
  }

  return (
    <div className={cn('rounded-md border border-input bg-transparent', className)}>
      <div className="flex flex-wrap items-center gap-1 border-b px-2 py-1.5">
        <ToolbarButton label="Bold" onClick={() => exec('bold')}>
          <Bold />
        </ToolbarButton>
        <ToolbarButton label="Italic" onClick={() => exec('italic')}>
          <Italic />
        </ToolbarButton>
        <ToolbarButton label="Bullet list" onClick={() => exec('insertUnorderedList')}>
          <List />
        </ToolbarButton>
        {showLink && (
          <ToolbarButton label="Add link" onClick={addLink}>
            <Link2 />
          </ToolbarButton>
        )}
        {attributes.length > 0 && (
          <>
            <span className="mx-1 h-5 w-px bg-border" />
            <div className="relative">
          <button
            type="button"
            onMouseDown={(e) => e.preventDefault()}
            onClick={() => setMenuOpen((open) => !open)}
            className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-muted-foreground hover:bg-accent hover:text-accent-foreground"
          >
            <Braces className="h-3.5 w-3.5" />
            Insert attribute
          </button>
          {menuOpen && (
            <div className="absolute left-0 z-20 mt-1 w-80 rounded-md border bg-white p-2 shadow-lg">
              {attributes.map((group) => (
                <div key={group.label} className="mb-1 last:mb-0">
                  <p className="px-2 py-1 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
                    {group.label}
                  </p>
                  {group.items.map((item) => (
                    <button
                      key={item.token}
                      type="button"
                      onMouseDown={(e) => e.preventDefault()}
                      onClick={() => insertToken(item.token)}
                      className="block w-full rounded px-2 py-1.5 text-left text-sm hover:bg-accent"
                    >
                      {item.label}
                      <span className="ml-2 font-mono text-[11px] text-muted-foreground">{item.token}</span>
                    </button>
                  ))}
                </div>
              ))}
            </div>
          )}
        </div>
          </>
        )}
      </div>

      <div
        ref={editorRef}
        contentEditable
        role="textbox"
        aria-multiline="true"
        data-placeholder={placeholder}
        onInput={emit}
        onBlur={emit}
        suppressContentEditableWarning
        className="rich-text-editor min-h-[220px] px-3 py-2 text-sm outline-none [&_a]:text-primary [&_a]:underline [&_ol]:list-decimal [&_ol]:pl-5 [&_ul]:list-disc [&_ul]:pl-5"
      />
    </div>
  )
}

function ToolbarButton({
  label,
  onClick,
  children,
}: {
  label: string
  onClick: () => void
  children: ReactNode
}) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      onMouseDown={(e) => e.preventDefault()}
      onClick={onClick}
      className="inline-flex h-7 w-7 items-center justify-center rounded text-muted-foreground hover:bg-accent hover:text-accent-foreground [&_svg]:h-4 [&_svg]:w-4"
    >
      {children}
    </button>
  )
}
