import { useState } from 'react'
import { UserCheck, Megaphone, Search, ListOrdered, Briefcase, Building2, BarChart3, ShieldCheck } from 'lucide-react'
import { SearchPage } from '@/features/leads/SearchPage'
import { LeadsPage } from '@/features/leads/LeadsPage'
import { CampaignsPage } from '@/features/campaigns/CampaignsPage'
import { CampaignDetailPage } from '@/features/campaigns/CampaignDetailPage'
import { SequencesPage } from '@/features/sequences/SequencesPage'
import { SequenceEditorPage } from '@/features/sequences/SequenceEditorPage'
import { JobsPage } from '@/features/jobs/JobsPage'
import { JobEditorPage } from '@/features/jobs/JobEditorPage'
import { OrganizationPage } from '@/features/admin/OrganizationPage'
import { GuardrailsPage } from '@/features/admin/GuardrailsPage'
import { ReportsPage } from '@/features/reports/ReportsPage'
import ScottyAssistant from '@/components/ScottyAssistant'
import { cn } from '@/lib/utils'
import type { ProfileSearchRequest } from '@/types/models'

type View =
  | { page: 'search'; initialCriteria?: ProfileSearchRequest }
  | { page: 'leads' | 'campaigns' | 'sequences' | 'jobs' | 'organization' | 'reports' | 'guardrails' }
  | { page: 'campaign-detail'; campaignId: number }
  | { page: 'sequence-detail'; sequenceId?: number }
  | { page: 'job-detail'; jobId?: number }

const nav = [
  { key: 'search', label: 'Search', icon: Search },
  { key: 'leads', label: 'Your Leads', icon: UserCheck },
  { key: 'campaigns', label: 'Campaigns', icon: Megaphone },
  { key: 'sequences', label: 'Sequences', icon: ListOrdered },
  { key: 'jobs', label: 'Jobs', icon: Briefcase },
  { key: 'organization', label: 'Organization', icon: Building2 },
  { key: 'guardrails', label: 'Guardrails', icon: ShieldCheck },
  { key: 'reports', label: 'Reports', icon: BarChart3 },
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
                view.page === key ||
                  (view.page === 'campaign-detail' && key === 'campaigns') ||
                  (view.page === 'sequence-detail' && key === 'sequences') ||
                  (view.page === 'job-detail' && key === 'jobs') ||
                  (view.page === 'organization' && key === 'organization') ||
                  (view.page === 'guardrails' && key === 'guardrails') ||
                  (view.page === 'reports' && key === 'reports')
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
          {view.page === 'search' && <SearchPage initialCriteria={view.initialCriteria} />}
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
          {view.page === 'sequences' && (
            <SequencesPage
              onNewSequence={() => setView({ page: 'sequence-detail' })}
              onOpenSequence={(sequenceId) => setView({ page: 'sequence-detail', sequenceId })}
            />
          )}
          {view.page === 'sequence-detail' && (
            <SequenceEditorPage
              key={view.sequenceId ?? 'new'}
              sequenceId={view.sequenceId}
              onBack={() => setView({ page: 'sequences' })}
              onSaved={(sequenceId) => setView({ page: 'sequence-detail', sequenceId })}
            />
          )}
          {view.page === 'jobs' && (
            <JobsPage
              onNewJob={() => setView({ page: 'job-detail' })}
              onOpenJob={(jobId) => setView({ page: 'job-detail', jobId })}
            />
          )}
          {view.page === 'job-detail' && (
            <JobEditorPage
              key={view.jobId ?? 'new'}
              jobId={view.jobId}
              onBack={() => setView({ page: 'jobs' })}
              onSaved={(jobId) => setView({ page: 'job-detail', jobId })}
              onRunSearch={(_jobId, criteria) => {
                setView({ page: 'search', initialCriteria: criteria })
              }}
            />
          )}
          {view.page === 'organization' && <OrganizationPage />}
          {view.page === 'guardrails' && <GuardrailsPage />}
          {view.page === 'reports' && <ReportsPage />}
        </div>
      </main>

      <ScottyAssistant />
    </div>
  )
}

export default App
