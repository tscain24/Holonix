import { Component } from '@angular/core';
import { Location } from '@angular/common';
import { Router } from '@angular/router';

interface TermsSection {
  title: string;
  bullets?: string[];
  paragraphs?: string[];
}

interface TermsHighlight {
  title: string;
  detail: string;
}

@Component({
  selector: 'app-terms-of-service',
  templateUrl: './terms-of-service.component.html',
  styleUrls: ['./terms-of-service.component.css'],
})
export class TermsOfServiceComponent {
  readonly lastUpdated = 'July 3, 2026';
  readonly highlights: TermsHighlight[] = [
    {
      title: 'Marketplace only',
      detail: 'Holonix connects customers and independent service providers, but does not perform or supervise the work itself.',
    },
    {
      title: 'Booking and payment',
      detail: 'Bookings are confirmed only after provider acceptance, and payments may be authorized immediately then captured closer to service time.',
    },
    {
      title: 'Independent providers',
      detail: 'Service providers remain responsible for licensing, insurance, taxes, compliance, and work quality.',
    },
    {
      title: 'Legal protections',
      detail: 'These Terms allocate risk, limit liability, and require users to comply with platform rules and applicable law.',
    },
  ];

  readonly sections: TermsSection[] = [
    {
      title: '1. Definitions',
      paragraphs: [
        'These Terms apply to the Holonix platform, including our website, booking flows, related applications, and connected services.',
        'For purposes of these Terms, references to Customer, Service Provider, Platform, Booking, Services, Content, Account, and Payment Processor should be interpreted consistently with Holonix\'s role as a technology marketplace.',
        'These Terms are intended to allocate responsibility for the performance of services to independent service providers rather than Holonix, and jurisdiction-specific language may be added or revised by legal counsel.',
      ],
    },
    {
      title: '2. Eligibility and Accounts',
      bullets: [
        'Users must be at least 18 years old.',
        'Users are responsible for maintaining the security of their accounts.',
        'Users must provide accurate, current, and complete information.',
      ],
      paragraphs: [
        'Holonix may rely on the information provided through each account and may suspend or restrict access where account information is false, misleading, or incomplete.',
      ],
    },
    {
      title: '3. Marketplace Role',
      bullets: [
        'Holonix is a technology marketplace only.',
        'Holonix is not the employer, contractor, subcontractor, or agent of any Service Provider.',
        'Holonix does not supervise, inspect, or control services performed by Service Providers.',
      ],
    },
    {
      title: '4. Booking Process',
      bullets: [
        'Customers submit booking requests through the platform.',
        'Providers may accept or decline requests.',
        'A booking is not confirmed until it is accepted.',
        'Holonix may cancel bookings for suspected fraud, payment failures, or safety concerns.',
      ],
    },
    {
      title: '5. Pricing and Payments',
      bullets: [
        'Pricing is displayed before checkout.',
        'Customers authorize payment processing through Stripe.',
        'Payment may be authorized immediately and captured up to 24 hours before service.',
        'Holonix may collect and remit funds while deducting agreed platform fees.',
      ],
    },
    {
      title: '6. Cancellations and Refunds',
      bullets: [
        'Cancellation policies are displayed during booking.',
        'Refund eligibility depends on the applicable cancellation policy.',
        'Holonix may facilitate refunds but is not obligated to resolve workmanship disputes.',
      ],
    },
    {
      title: '7. Independent Service Providers',
      bullets: [
        'Providers are solely responsible for licensing, insurance, taxes, employees, subcontractors, equipment, and legal compliance.',
        'Providers represent and warrant that they possess all legally required licenses and insurance.',
      ],
    },
    {
      title: '8. Customer Responsibilities',
      bullets: [
        'Provide accurate addresses and booking details.',
        'Provide safe site access for scheduled work.',
        'Disclose known hazards relevant to the requested work.',
        'Treat providers respectfully.',
        'Do not request unlawful work.',
      ],
    },
    {
      title: '9. Provider Responsibilities',
      bullets: [
        'Perform services professionally.',
        'Maintain required insurance and credentials.',
        'Comply with applicable laws and regulations.',
        'Honor accepted bookings unless canceled appropriately through the platform.',
      ],
    },
    {
      title: '10. Reviews and User Content',
      bullets: [
        'Users grant Holonix a license to display reviews, ratings, and uploaded content.',
        'Users may not post unlawful, infringing, or otherwise prohibited content.',
      ],
    },
    {
      title: '11. Intellectual Property',
      bullets: [
        'Holonix trademarks, branding, software, and platform content remain the property of Holonix.',
        'No license is granted except as necessary to use the platform in accordance with these Terms.',
      ],
    },
    {
      title: '12. Privacy',
      bullets: [
        'Use of the platform is subject to the Holonix Privacy Policy.',
        'Payment information is processed by Stripe and is not stored by Holonix except as required for platform operations and compliance.',
      ],
    },
    {
      title: '13. Disclaimer of Warranties',
      bullets: [
        'The platform is provided on an "AS IS" and "AS AVAILABLE" basis.',
        'Holonix makes no guarantee regarding service quality, provider availability, completion, or outcomes.',
      ],
    },
    {
      title: '14. Limitation of Liability',
      bullets: [
        'To the fullest extent permitted by law, Holonix is not liable for negligence, property damage, bodily injury, delays, cancellations, workmanship defects, or disputes arising from services performed by independent providers.',
        'Holonix\'s aggregate liability is limited to the amount paid through the platform for the booking giving rise to the claim.',
      ],
    },
    {
      title: '15. Indemnification',
      paragraphs: [
        'Customers and providers agree to defend, indemnify, and hold Holonix harmless from claims arising out of their conduct, services, violations of law, or breach of these Terms.',
      ],
    },
    {
      title: '16. Dispute Resolution',
      bullets: [
        'Users should attempt informal resolution first.',
        'Any optional arbitration or class-action waiver language should be reviewed and finalized by counsel before release.',
        'Governing law and venue should be specified once selected by Holonix.',
      ],
    },
    {
      title: '17. Suspension and Termination',
      paragraphs: [
        'Holonix may suspend or terminate accounts for fraud, abuse, illegal activity, or violations of these Terms.',
      ],
    },
    {
      title: '18. Changes to the Terms',
      bullets: [
        'Holonix may update these Terms at any time.',
        'Material changes become effective upon posting or as otherwise communicated.',
      ],
    },
    {
      title: '19. Contact Information',
      paragraphs: [
        'For questions about these Terms, contact Holonix at support@holonix.com.',
      ],
    },
  ];

  constructor(
    private readonly location: Location,
    private readonly router: Router
  ) {}

  getSectionId(index: number): string {
    return `terms-section-${index}`;
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
