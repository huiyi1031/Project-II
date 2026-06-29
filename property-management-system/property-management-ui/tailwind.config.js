/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        navy:             '#0b2d5c',
        blue:             '#1f5fae',
        'light-blue':     '#eaf3ff',
        'mid-blue':       '#cfe3ff',
        'pms-text':       '#1b2a41',
        muted:            '#6b7a90',
        success:          '#22a06b',
        warning:          '#e2a400',
        danger:           '#d64545',
        card:             '#ffffff',
        bg:               '#f5f9ff',
        border:           '#dbe7fb',
        'status-pending-bg':    '#fff4da',
        'status-pending-text':  '#925e00',
        'status-assigned-bg':   '#e6f0ff',
        'status-assigned-text': '#154a9a',
        'status-progress-bg':   '#e5fbff',
        'status-progress-text': '#0b5f6e',
        'status-complete-bg':   '#e7f8ef',
        'status-complete-text': '#157347',
        'status-cancel-bg':     '#ffe7e7',
        'status-cancel-text':   '#a32626',
      },
      fontFamily: {
        sans: ['"Segoe UI"', 'Roboto', 'Arial', 'sans-serif'],
      },
      borderRadius: {
        card: '14px',
        xl2: '20px',
      },
      boxShadow: {
        card:  '0 8px 24px rgba(11,45,92,.08)',
        focus: '0 0 0 3px rgba(31,95,174,.12)',
      },
      backgroundImage: {
        'hero-gradient':    'linear-gradient(145deg, #0b2d5c, #1f5fae)',
        'sidebar-gradient': 'linear-gradient(180deg, #0b2d5c, #123f79)',
        'btn-gradient':     'linear-gradient(135deg, #1f5fae, #2f7de0)',
        'page-gradient':    'linear-gradient(135deg, #f6fbff 0%, #ebf4ff 100%)',
      },
    },
  },
  plugins: [],
}
