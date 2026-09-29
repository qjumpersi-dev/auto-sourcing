import { useState, type ReactNode } from 'react'
import { Award, BookOpen, Briefcase, Building2, Globe, GraduationCap, Lightbulb, Loader2, Pencil, Sparkles, Target, Trophy, Users, Wrench, X } from 'lucide-react'
import { useGetLeadConsentQuery, useGetLeadProfileQuery } from '@/services/apiSlice'
import {
  consentChannelLabels,
  consentStatusLabels,
  ConsentChannel,
  type Lead,
} from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { LeadEditModal } from './LeadEditModal'

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

// Formats an ISO-ish date (e.g. "2005-12-01 00:00:00") as dd-mmm-yyyy.
function formatProfileDate(value: string | null | undefined): string {
  if (!value) return '—'
  const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/)
  if (!match) return value
  const [, year, month, day] = match
  const monthIndex = Number(month) - 1
  if (monthIndex < 0 || monthIndex > 11) return value
  return `${day}-${MONTHS[monthIndex]}-${year}`
}

interface RhetorikFallback {
  headline?: string | null
  summary?: string | null
  selfReportedSkills?: string[] | null
  aiInferredSkills?: string[] | null
  workExperience?: { company?: string | null; title?: string | null; current?: boolean; startDate?: string | null; endDate?: string | null }[] | null
  education?: { school?: string | null; degree?: string | null; specialization?: string | null; startDate?: string | null; endDate?: string | null }[] | null
  certifications?: string[] | null
  industries?: string[] | null
  languages?: string[] | null
  memberships?: string[] | null
  publications?: string[] | null
  awards?: string[] | null
  patents?: string[] | null
}

export function LeadProfileModal({
  lead,
  rhetorikData,
  onClose,
}: {
  lead: Lead
  rhetorikData?: RhetorikFallback
  onClose: () => void
}) {
  const { data: consent, isLoading: consentLoading } = useGetLeadConsentQuery(lead.id)
  const { data: profile } = useGetLeadProfileQuery(lead.id, { skip: !lead.id })
  const [showEdit, setShowEdit] = useState(false)

  const parseList = (value: string | null | undefined): string[] => {
    if (!value) return []
    try {
      const parsed = JSON.parse(value)
      return Array.isArray(parsed) ? parsed : []
    } catch {
      return value.split(',').map((s) => s.trim()).filter(Boolean)
    }
  }

  const parseWorkExperience = (value: string | null | undefined): { company?: string; title?: string; current?: boolean; startDate?: string; endDate?: string }[] => {
    if (!value) return []
    try {
      const parsed = JSON.parse(value)
      return Array.isArray(parsed) ? parsed : []
    } catch {
      return []
    }
  }

  const formatEducation = (e: { school?: string | null; degree?: string | null; specialization?: string | null; startDate?: string | null; endDate?: string | null }): string => {
    const degree = e.degree || ''
    const specialization = e.specialization ? ` in ${e.specialization}` : ''
    const school = e.school ? ` at ${e.school}` : ''
    let dates = ''
    if (e.startDate || e.endDate) {
      const start = e.startDate ? formatProfileDate(e.startDate) : '?'
      const end = e.endDate ? formatProfileDate(e.endDate) : 'Current'
      dates = ` (${start} – ${end})`
    }
    return `${degree}${specialization}${school}${dates}`.trim() || '—'
  }

  const parseEducation = (value: string | null | undefined): string[] => {
    if (!value) return []
    try {
      const parsed = JSON.parse(value)
      return Array.isArray(parsed) ? parsed.map(formatEducation) : []
    } catch {
      return value.split(',').map((s) => s.trim()).filter(Boolean)
    }
  }

  // Use profile data from DB if available, otherwise fall back to Rhetorik search data
  const headline = profile?.headline ?? rhetorikData?.headline ?? lead.jobTitle
  const summary = profile?.summary ?? rhetorikData?.summary
  const skills = profile?.selfReportedSkills
    ? parseList(profile.selfReportedSkills)
    : (rhetorikData?.selfReportedSkills ?? [])
  const aiSkills = profile?.aiInferredSkills
    ? parseList(profile.aiInferredSkills)
    : (rhetorikData?.aiInferredSkills ?? [])
  const workExperience = profile?.workExperience
    ? parseWorkExperience(profile.workExperience)
    : (rhetorikData?.workExperience ?? [])
  const certifications = profile?.certifications
    ? parseList(profile.certifications)
    : (rhetorikData?.certifications ?? [])
  const industries = profile?.industries
    ? parseList(profile.industries)
    : (rhetorikData?.industries ?? [])
  const languages = profile?.languages
    ? parseList(profile.languages)
    : (rhetorikData?.languages ?? [])
  const memberships = profile?.memberships
    ? parseList(profile.memberships)
    : (rhetorikData?.memberships ?? [])
  const publications = profile?.publications
    ? parseList(profile.publications)
    : (rhetorikData?.publications ?? [])
  const awards = profile?.awards
    ? parseList(profile.awards)
    : (rhetorikData?.awards ?? [])
  const patents = profile?.patents
    ? parseList(profile.patents)
    : (rhetorikData?.patents ?? [])
  const education = profile?.education
    ? parseEducation(profile.education)
    : (rhetorikData?.education?.map(formatEducation) ?? [])

  return (
    <>
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
        <div className="max-h-[90vh] w-full max-w-3xl overflow-y-auto rounded-lg border bg-white p-6 shadow-xl">
          <div className="mb-4 flex items-center justify-between">
            <div>
              <h3 className="text-lg font-semibold">
                {lead.firstName} {lead.lastName}
              </h3>
              <p className="text-sm text-muted-foreground">{headline || lead.jobTitle || ''}</p>
              <p className="text-sm text-muted-foreground">
                {[lead.location, lead.company].filter(Boolean).join(' · ')}
              </p>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => setShowEdit(true)}>
                <Pencil />
                Edit
              </Button>
              <Button variant="ghost" size="icon" onClick={onClose} aria-label="Close">
                <X />
              </Button>
            </div>
          </div>

          <div className="space-y-6">
            {/* Contact details */}
            <Section title="Contact details" icon={<Users className="h-4 w-4" />}>
              <div className="grid gap-3 sm:grid-cols-2">
                <Field label="First name" value={lead.firstName} />
                <Field label="Last name" value={lead.lastName} />
                <Field label="Email" value={lead.email} />
                <Field label="Mobile phone" value={lead.phone} />
                <Field label="LinkedIn URL" value={lead.linkedInUrl} />
                <Field label="Country" value={lead.country} />
              </div>
            </Section>

            {/* Summary */}
            {summary && (
              <Section title="Summary" icon={<BookOpen className="h-4 w-4" />}>
                <p className="text-sm whitespace-pre-wrap">{summary}</p>
              </Section>
            )}

            {/* Self Reported Skills */}
            <Section title="Self Reported Skills" icon={<Wrench className="h-4 w-4" />}>
              {skills.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {skills.map((skill, i) => (
                    <Badge key={i} variant="outline">{skill}</Badge>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* AI Inferred Skills */}
            <Section title="AI Inferred Skills" icon={<Sparkles className="h-4 w-4" />}>
              {aiSkills.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {aiSkills.map((skill, i) => (
                    <Badge key={i} variant="secondary">{skill}</Badge>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Work Experience */}
            <Section title="Work Experience" icon={<Briefcase className="h-4 w-4" />}>
              {workExperience.length > 0 ? (
                <div className="space-y-4">
                  {workExperience.map((exp, i) => (
                    <div key={i} className="flex items-start gap-3">
                      <div className="mt-2 h-2 w-2 rounded-full bg-blue-600 shrink-0" />
                      <div>
                        <p className="font-medium">{exp.company}</p>
                        <p className="text-sm text-muted-foreground">{exp.title}</p>
                        <p className="text-xs text-muted-foreground">
                          {formatProfileDate(exp.startDate)} – {exp.current ? 'Current' : formatProfileDate(exp.endDate)}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Education */}
            <Section title="Education" icon={<GraduationCap className="h-4 w-4" />}>
              {education.length > 0 ? (
                <div className="space-y-1">
                  {education.map((edu, i) => (
                    <p key={i} className="text-sm">{edu}</p>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Certifications */}
            <Section title="Certifications" icon={<Award className="h-4 w-4" />}>
              {certifications.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {certifications.map((cert, i) => (
                    <Badge key={i} variant="outline">{cert}</Badge>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Industries */}
            <Section title="Industries" icon={<Building2 className="h-4 w-4" />}>
              {industries.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {industries.map((ind, i) => (
                    <Badge key={i} variant="outline">{ind}</Badge>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Languages */}
            <Section title="Languages" icon={<Globe className="h-4 w-4" />}>
              {languages.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {languages.map((lang, i) => (
                    <Badge key={i} variant="outline">{lang}</Badge>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Memberships */}
            <Section title="Memberships" icon={<Users className="h-4 w-4" />}>
              {memberships.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {memberships.map((m, i) => (
                    <Badge key={i} variant="outline">{m}</Badge>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Publications */}
            <Section title="Publications" icon={<BookOpen className="h-4 w-4" />}>
              {publications.length > 0 ? (
                <div className="space-y-1">
                  {publications.map((pub, i) => (
                    <p key={i} className="text-sm">{pub}</p>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Awards */}
            <Section title="Awards" icon={<Trophy className="h-4 w-4" />}>
              {awards.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {awards.map((award, i) => (
                    <Badge key={i} variant="outline">{award}</Badge>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Patents */}
            <Section title="Patents" icon={<Lightbulb className="h-4 w-4" />}>
              {patents.length > 0 ? (
                <div className="space-y-1">
                  {patents.map((patent, i) => (
                    <p key={i} className="text-sm">{patent}</p>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Consent */}
            <Section title="Channel consent" icon={<Target className="h-4 w-4" />}>
              {consentLoading ? (
                <p className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Loader2 className="animate-spin" /> Loading...
                </p>
              ) : consent ? (
                <div className="space-y-2">
                  <div className="flex gap-4 text-sm">
                    <span>
                      <span className="font-medium">Preferred channel:</span>{' '}
                      {consent.preferredChannel !== null && consent.preferredChannel !== undefined
                        ? consentChannelLabels[consent.preferredChannel]
                        : 'Not set'}
                    </span>
                  </div>
                  {[ConsentChannel.Email, ConsentChannel.Sms, ConsentChannel.LinkedIn].map((channel) => {
                    const c = consent.consents.find((x) => x.channel === channel)
                    return (
                      <div key={channel} className="flex items-center gap-3 text-sm">
                        <span className="w-20 font-medium">{consentChannelLabels[channel]}</span>
                        <Badge
                          variant={
                            c?.status === 1
                              ? 'success'
                              : c?.status === 2 || c?.status === 3
                                ? 'destructive'
                                : 'secondary'
                          }
                        >
                          {c ? consentStatusLabels[c.status] : 'Unknown'}
                        </Badge>
                        {c?.optInSource && (
                          <span className="text-xs text-muted-foreground">via {c.optInSource}</span>
                        )}
                      </div>
                    )
                  })}
                </div>
              ) : (
                <Empty />
              )}
            </Section>

            {/* Campaigns */}
            <Section title="Campaigns" icon={<Briefcase className="h-4 w-4" />}>
              {lead.campaigns && lead.campaigns.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {lead.campaigns.map((c) => (
                    <Badge key={c.id} variant="outline">{c.name}</Badge>
                  ))}
                </div>
              ) : (
                <Empty />
              )}
            </Section>
          </div>
        </div>
      </div>

      {showEdit && <LeadEditModal lead={lead} onClose={() => setShowEdit(false)} />}
    </>
  )
}

function Section({ title, icon, children }: { title: string; icon?: ReactNode; children: ReactNode }) {
  return (
    <section>
      <h4 className="mb-2 flex items-center gap-2 text-sm font-semibold text-muted-foreground uppercase tracking-wide">
        {icon && <span className="text-blue-600">{icon}</span>}
        {title}
      </h4>
      {children}
    </section>
  )
}

function Field({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div>
      <Label className="text-xs text-muted-foreground">{label}</Label>
      <p className="text-sm">{value || <span className="text-muted-foreground">—</span>}</p>
    </div>
  )
}

function Empty() {
  return <p className="text-sm text-muted-foreground">—</p>
}
