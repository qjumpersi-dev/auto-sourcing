import { useState } from 'react'
import { UserCheck, Megaphone, Search } from 'lucide-react'
import { SearchPage } from '@/features/leads/SearchPage'
import { LeadsPage } from '@/features/leads/LeadsPage'
import { CampaignsPage } from '@/features/campaigns/CampaignsPage'
import { CampaignDetailPage } from '@/features/campaigns/CampaignDetailPage'
import ScottyAssistant from '@/components/ScottyAssistant'
import { cn } from '@/lib/utils'

type View = { page: 'search' | 'leads' | 'campaigns' } | { page: 'campaign-detail'; campaignId: number }

const nav = [
  { key: 'search', label: 'Search', icon: Search },
  { key: 'leads', label: 'Your Leads', icon: UserCheck },
  { key: 'campaigns', label: 'Campaigns', icon: Megaphone },
] as const

function App() {
  const [view, setView] = useState<View>({ page: 'search' })

  return (
    <div className="flex min-h-screen">
        <aside className="flex w-64 flex-col border-r bg-white">
          <div className="border-b px-5 py-5">
            <img
              src="/qjumpers-logo.png"
              alt="QJumpers"
              className="h-8 w-auto"
            />
            <p className="mt-2 text-xs text-muted-foreground">AI Sourcing & Recruiting</p>
          </div>
        <nav className="flex flex-col gap-1 p-3">
          {nav.map(({ key, label, icon: Icon }) => (
            <button
              key={key}
              type="button"
              onClick={() => setView({ page: key })}
              className={cn(
                'flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                view.page === key || (view.page === 'campaign-detail' && key === 'campaigns')
                  ? 'bg-accent text-accent-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
              )}
            >
              <Icon />
              {label}
            </button>
          ))}
        </nav>
      </aside>

      <main className="flex-1 p-8">
        <div className="mx-auto max-w-6xl">
          {view.page === 'search' && <SearchPage />}
          {view.page === 'leads' && <LeadsPage />}
          {view.page === 'campaigns' && (
            <CampaignsPage onOpenCampaign={(campaignId) => setView({ page: 'campaign-detail', campaignId })} />
          )}
          {view.page === 'campaign-detail' && (
            <CampaignDetailPage
              campaignId={view.campaignId}
              onBack={() => setView({ page: 'campaigns' })}
            />
          )}
        </div>
      </main>

      <ScottyAssistant />
    </div>
  )
}

export default App
