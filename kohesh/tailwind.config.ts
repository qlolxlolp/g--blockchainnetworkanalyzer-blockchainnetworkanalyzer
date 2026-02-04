
import type { Config } from "tailwindcss";

export default {
	darkMode: ["class"],
	content: [
		"./pages/**/*.{ts,tsx}",
		"./components/**/*.{ts,tsx}",
		"./app/**/*.{ts,tsx}",
		"./src/**/*.{ts,tsx}",
	],
	prefix: "",
	theme: {
		container: {
			center: true,
			padding: '2rem',
			screens: {
				'2xl': '1400px'
			}
		},
		extend: {
			colors: {
				border: 'rgb(128, 128, 128)',
				input: 'rgb(255, 255, 255)',
				ring: 'rgb(0, 0, 128)',
				background: 'rgb(240, 240, 240)',
				foreground: 'rgb(0, 0, 0)',
				primary: {
					DEFAULT: 'rgb(0, 0, 128)',
					foreground: 'rgb(255, 255, 255)'
				},
				secondary: {
					DEFAULT: 'rgb(192, 192, 192)',
					foreground: 'rgb(0, 0, 0)'
				},
				destructive: {
					DEFAULT: 'rgb(255, 0, 0)',
					foreground: 'rgb(255, 255, 255)'
				},
				muted: {
					DEFAULT: 'rgb(236, 233, 216)',
					foreground: 'rgb(64, 64, 64)'
				},
				accent: {
					DEFAULT: 'rgb(0, 0, 128)',
					foreground: 'rgb(255, 255, 255)'
				},
				popover: {
					DEFAULT: 'rgb(236, 233, 216)',
					foreground: 'rgb(0, 0, 0)'
				},
				card: {
					DEFAULT: 'rgb(236, 233, 216)',
					foreground: 'rgb(0, 0, 0)'
				},
				sidebar: {
					DEFAULT: 'rgb(192, 192, 192)',
					foreground: 'rgb(0, 0, 0)',
					primary: 'rgb(0, 0, 128)',
					'primary-foreground': 'rgb(255, 255, 255)',
					accent: 'rgb(236, 233, 216)',
					'accent-foreground': 'rgb(0, 0, 0)',
					border: 'rgb(128, 128, 128)',
					ring: 'rgb(0, 0, 128)'
				}
			},
			borderRadius: {
				lg: '0px',
				md: '0px',
				sm: '0px'
			},
			keyframes: {
				'accordion-down': {
					from: {
						height: '0'
					},
					to: {
						height: 'var(--radix-accordion-content-height)'
					}
				},
				'accordion-up': {
					from: {
						height: 'var(--radix-accordion-content-height)'
					},
					to: {
						height: '0'
					}
				}
			},
			animation: {
				'accordion-down': 'accordion-down 0.2s ease-out',
				'accordion-up': 'accordion-up 0.2s ease-out'
			}
		}
	},
	plugins: [require("tailwindcss-animate")],
} satisfies Config;
