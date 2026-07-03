import { Component } from '@angular/core';
import { Location } from '@angular/common';
import { Router } from '@angular/router';

interface PrivacySection {
  title: string;
  paragraphs?: string[];
}

interface PrivacyHighlight {
  title: string;
  detail: string;
}

@Component({
  selector: 'app-privacy-policy',
  templateUrl: './privacy-policy.component.html',
  styleUrls: ['./privacy-policy.component.css'],
})
export class PrivacyPolicyComponent {
  readonly effectiveDate = 'July 3, 2026';
  readonly lastUpdated = 'July 3, 2026';

  readonly highlights: PrivacyHighlight[] = [
    {
      title: 'What we collect',
      detail: 'Account details, booking information, support messages, device metadata, and related platform activity.',
    },
    {
      title: 'How we use it',
      detail: 'To operate accounts, support bookings, process payments, prevent fraud, improve the platform, and comply with law.',
    },
    {
      title: 'How we share it',
      detail: 'With the selected service provider and trusted vendors that support payments, hosting, analytics, communications, and security.',
    },
    {
      title: 'Your choices',
      detail: 'Depending on your jurisdiction, you may request access, correction, deletion, copies of data, or opt-outs from certain processing.',
    },
  ];

  readonly sections: PrivacySection[] = [
    {
      title: '1. Scope of this Policy',
      paragraphs: [
        'This Policy applies to information collected through the Holonix platform, customer support interactions, booking requests, provider onboarding, and communications.',
        'It does not apply to information collected directly by independent service providers outside the Holonix platform.',
      ],
    },
    {
      title: '2. Information We Collect',
      paragraphs: [
        'We collect information you provide directly, including your name, email address, telephone number, service address, billing address, account credentials, profile details, booking requests, uploaded photos, communications, reviews, and customer support inquiries.',
        'We automatically collect technical information such as IP address, browser type, device identifiers, operating system, referring URLs, session data, and diagnostic logs.',
        'When payments are processed, Stripe provides us with transaction metadata, payment status, and identifiers necessary to administer bookings. We do not receive or store full payment card numbers or security codes.',
      ],
    },
    {
      title: '3. How We Use Information',
      paragraphs: [
        'We use personal information to create accounts, process bookings, communicate with users, facilitate payments, detect fraud, investigate disputes, improve platform performance, personalize the user experience, comply with legal obligations, and enforce our agreements.',
        'We may also use aggregated and de-identified information for analytics and product improvement.',
      ],
    },
    {
      title: '4. How Information Is Shared',
      paragraphs: [
        'Information necessary to fulfill a booking is shared with the selected independent service provider.',
        'We also share information with vendors that provide payment processing, cloud hosting, analytics, customer support, communications, fraud prevention, and infrastructure services.',
        'Information may be disclosed if required by law, to protect legal rights, or as part of a merger, acquisition, or sale of assets.',
        'Holonix does not sell personal information for monetary consideration.',
      ],
    },
    {
      title: '5. Cookies and Similar Technologies',
      paragraphs: [
        'We use cookies, local storage, and similar technologies to authenticate users, remember preferences, maintain security, analyze platform usage, and improve functionality.',
        'You may control cookies through your browser settings, although some features may not function properly if cookies are disabled.',
      ],
    },
    {
      title: '6. Data Retention',
      paragraphs: [
        'We retain information for as long as reasonably necessary to provide our services, satisfy legal and accounting obligations, resolve disputes, enforce agreements, prevent fraud, and maintain business records.',
        'Retention periods vary depending on the type of information and applicable legal requirements.',
      ],
    },
    {
      title: '7. Data Security',
      paragraphs: [
        'Holonix implements administrative, technical, and physical safeguards designed to protect personal information from unauthorized access, disclosure, alteration, or destruction.',
        'While we strive to protect your information, no internet-based service or electronic storage system can be guaranteed to be completely secure.',
      ],
    },
    {
      title: '8. Your Privacy Rights',
      paragraphs: [
        'Depending on where you reside, you may have the right to request access to your personal information, request correction or deletion, obtain a copy of your data, object to certain processing activities, or opt out of marketing communications.',
        'Requests may be submitted using the contact information below.',
      ],
    },
    {
      title: '9. Children\'s Privacy',
      paragraphs: [
        'Holonix is intended for individuals who are at least 18 years old. We do not knowingly collect personal information from children.',
        'If we become aware that information has been collected from a child, we will take reasonable steps to delete it.',
      ],
    },
    {
      title: '10. Changes to this Policy',
      paragraphs: [
        'We may update this Privacy Policy from time to time to reflect changes in our practices, technology, legal requirements, or business operations.',
        'The revised version will be posted on the Platform with an updated effective date.',
      ],
    },
    {
      title: '11. Contact Us',
      paragraphs: [
        'Questions regarding this Privacy Policy or requests relating to your personal information may be directed to the Holonix Privacy Team at info@holonix.io.',
      ],
    },
  ];

  constructor(
    private readonly location: Location,
    private readonly router: Router
  ) {}

  getSectionId(index: number): string {
    return `privacy-section-${index}`;
  }

  getSectionLabel(title: string): string {
    return title.replace(/^\d+\.\s*/, '').trim();
  }

  scrollToSection(index: number): void {
    const id = this.getSectionId(index);
    const target = typeof document !== 'undefined' ? document.getElementById(id) : null;
    const basePath = this.router.url.split('#')[0];
    this.location.replaceState(`${basePath}#${id}`);

    if (!target) {
      return;
    }

    const headerOffset = 108;
    target.scrollIntoView({
      behavior: 'smooth',
      block: 'start',
    });

    window.setTimeout(() => {
      window.scrollBy({
        top: -headerOffset,
        left: 0,
        behavior: 'smooth',
      });
    }, 40);
  }
}
