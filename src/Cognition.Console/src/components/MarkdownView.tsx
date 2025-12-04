import React from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import remarkBreaks from 'remark-breaks'
import remarkMath from 'remark-math'
import rehypeRaw from 'rehype-raw'
import rehypeSanitize, { defaultSchema } from 'rehype-sanitize'
import rehypeHighlight from 'rehype-highlight'
import 'highlight.js/styles/github-dark.css'
import 'katex/dist/katex.min.css'

type Props = {
  content: string
}

// Extend sanitize schema to allow code block className for syntax highlighting
const schema = {
  ...defaultSchema,
  attributes: {
    ...defaultSchema.attributes,
    code: [
      ...(defaultSchema.attributes?.code || []),
      // Allow className like "language-ts"
      ['className']
    ],
    span: [
      ...(defaultSchema.attributes?.span || []),
      ['className']
    ]
  }
} as const

export default function MarkdownView({ content }: Props) {
  return (
    <div className="md-body">
      <ReactMarkdown
        remarkPlugins={[remarkGfm, remarkBreaks, remarkMath]}
        rehypePlugins={[
          rehypeRaw, // allow embedded HTML blocks (sanitized below)
          [rehypeSanitize, schema],
          rehypeHighlight,
        ]}
        components={{
          a({ node: _node, href, ...props }) {
            const url = String(href || '').trim();
            const lower = url.toLowerCase();
            const isHash = url.startsWith('#');
            const safe = isHash || lower.startsWith('http:') || lower.startsWith('https:') || lower.startsWith('mailto:') || lower.startsWith('tel:');
            if (!safe) {
              // render inert text if unsafe protocol
              return <span {...props} />
            }
            return <a href={url} {...props} target="_blank" rel="noopener noreferrer" />
          }
        }}
      >
        {content || ''}
      </ReactMarkdown>
      <style>{`
        .md-body p { margin: 0.2rem 0; }
        .md-body pre { background: rgba(255,255,255,0.06); padding: 10px; border-radius: 6px; overflow: auto; max-width: 100%; }
        .md-body code { font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace; font-size: 0.95em; word-break: break-word; overflow-wrap: anywhere; }
        .md-body table { border-collapse: collapse; width: 100%; max-width: 100%; display: block; overflow-x: auto; }
        .md-body th, .md-body td { border: 1px solid rgba(255,255,255,0.12); padding: 6px 8px; }
        .md-body blockquote { border-left: 3px solid rgba(255,255,255,0.2); margin: 8px 0; padding: 4px 12px; color: rgba(255,255,255,0.75); }
        .md-body ul, .md-body ol { padding-left: 1.2rem; }
        .md-body hr { border: none; border-top: 1px solid rgba(255,255,255,0.12); margin: 12px 0; }
        .md-body img { max-width: 100%; height: auto; }
        .md-body { overflow-wrap: anywhere; word-break: break-word; }
      `}</style>
    </div>
  )
}
