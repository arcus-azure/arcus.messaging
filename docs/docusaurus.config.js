const lightCodeTheme = require('./src/prism/light');
const darkCodeTheme = require('./src/prism/dark');

/** @type {import('@docusaurus/types').DocusaurusConfig} */
module.exports = {
  title: 'Arcus - Messaging',
  url: 'https://messaging.arcus-azure.net',
  baseUrl: '/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  favicon: 'img/favicon.ico',
  organizationName: 'arcus-azure', // Usually your GitHub org/user name.
  projectName: 'Arcus - Messaging', // Usually your repo name.
  themeConfig: {
    image: 'img/arcus.jpg',
    navbar: {
      title: '',
      logo: {
        alt: 'Arcus',
        src: 'img/arcus.png',
        srcDark: 'img/arcus_for_dark.png',
      },
      items: [
        {
          type: 'dropdown',
          label: 'Arcus Messaging',
          position: 'left',
          items: [
            {
              label: 'Arcus Observability',
              href: 'https://observability.arcus-azure.net/'
            },
            {
              label: 'Arcus Testing',
              href: 'https://testing.arcus-azure.net/'
            },
            {
              label: 'Arcus Scripting',
              href: 'https://scripting.arcus-azure.net/'
            },
            {
              label: 'Arcus Security',
              href: 'https://security.arcus-azure.net/',
            }
          ]
        },
        {
          type: 'docsVersionDropdown',

          //// Optional
          position: 'right',
          // Add additional dropdown items at the beginning/end of the dropdown.
          dropdownItemsBefore: [],
          // Do not add the link active class when browsing docs.
          dropdownActiveClassDisabled: true,
          docsPluginId: 'default',
        },
        {
          type: 'search',
          position: 'right',
        },
        {
          href: 'https://github.com/arcus-azure/arcus.messaging',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Community',
          items: [
            {
              label: 'Github',
              href: 'https://github.com/arcus-azure/arcus.messaging',
            },
            {
              label: 'Contribution guide',
              href: 'https://github.com/arcus-azure/.github/blob/main/CONTRIBUTING.md'
            },
            {
              label: 'Report an issue',
              href: 'https://github.com/arcus-azure/arcus.messaging/issues/new/choose'
            },
            {
              label: 'Discuss an idea',
              href: 'https://github.com/arcus-azure/arcus.messaging/discussions/new/choose'
            }
          ],
        },
        {
          title: 'Features',
          items: [
            {
              label: 'Azure Service Bus messaging',
              to: 'Features/Azure/service-bus'
            },
            {
              label: 'TCP health probe',
              to: 'Features/tcp-health-probe'
            }
          ]
        },
        {
          title: 'Support',
          items: [
            {
              label: 'Getting started',
              to: 'getting-started'
            },
            {
              label: 'Migrate v0 to v1',
              to: 'Guides/migration-guide-v1.0'
            },
            {
              label: 'Migrate v2 to v3',
              to: 'Guides/migration-guide-v3.0'
            }
          ]
        }
      ],
      copyright: `Copyright © ${new Date().getFullYear()}, Arcus - Messaging maintained by Codit`,
    },
    prism: {
      theme: lightCodeTheme,
      darkTheme: darkCodeTheme,
      additionalLanguages: ['csharp', 'powershell', 'diff'],
    },
  },
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
          routeBasePath: "/",
          path: 'preview',
          sidebarCollapsible: false,
          // Please change this to your repo.
          editUrl:
            'https://github.com/arcus-azure/arcus.messaging/edit/master/docs',
          includeCurrentVersion: process.env.CONTEXT !== 'production',
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],
  stylesheets: [
    'https://fonts.googleapis.com/css2?family=Bitter:wght@700&family=Inter:wght@400;500&display=swap',
  ],
};
