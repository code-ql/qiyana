'use client'

import * as React from 'react'

const screenshots = [
    {
        title: '我的战绩面板',
        tag: 'Dashboard',
        desc: '自动识别当前账号，查看近期对局、胜负状态、模式筛选和详细战绩。',
        src: '/resource/1.png',
    },
    {
        title: '战绩查询',
        tag: 'Search',
        desc: '搜索召唤师并保留多标签结果，赛前快速查看队友或对手表现。',
        src: '/resource/2.png',
    },
    {
        title: '对局监听',
        tag: 'Live',
        desc: '英雄选择阶段读取队友段位、近期表现、补位状态和常用英雄。',
        src: '/resource/3_1.png',
    },
    {
        title: '实时阵容信息',
        tag: 'In Game',
        desc: '进入游戏后继续展示关键数据，帮助判断阵容强弱和玩家状态。',
        src: '/resource/3_2.png',
    },
]

const features = [
    ['01', '本机 LCU 连接', '连接已登录的英雄联盟客户端，不需要输入账号密码。'],
    ['02', '战绩面板', '把近期战绩、对局详情和模式筛选集中到一个页面。'],
    ['03', '对局监听', '从英雄选择到游戏中持续读取关键玩家信息。'],
]

export default function Page() {
    return (
        <main className="h-full min-h-0 overflow-hidden rounded-[2rem] bg-[#eef2f7] text-[#111827]">
            <div className="flex h-full min-h-0 flex-col overflow-hidden rounded-[2rem] border border-black/10 bg-[linear-gradient(135deg,#f8fafc_0%,#eef2ff_45%,#e0f2fe_100%)]">
                <header className="shrink-0 px-4 pt-4 sm:px-6">
                    <div className="flex items-center justify-between rounded-[1.5rem] border border-white bg-white/80 px-4 py-3 shadow-sm backdrop-blur-xl">
                        <div className="flex items-center gap-3">
                            <div className="grid h-11 w-11 place-items-center rounded-2xl bg-[#111827] text-lg font-black text-white shadow-lg shadow-slate-300">Q</div>
                            <div>
                                <p className="text-xs font-bold uppercase tracking-[0.28em] text-sky-600">League LCU Client</p>
                                <h1 className="text-lg font-black tracking-tight">Qiyana</h1>
                            </div>
                        </div>
                        <a
                            href="/download/qiyana.exe"
                            className="rounded-2xl bg-[#111827] px-5 py-2.5 text-sm font-black text-white shadow-lg shadow-slate-300 transition hover:-translate-y-0.5 hover:bg-sky-600"
                        >
                            下载 Windows 版
                        </a>
                    </div>
                </header>

                <div className="min-h-0 flex-1 overflow-y-auto px-4 pb-4 pt-4 sm:px-6 sm:pb-6">
                    <section className="grid gap-4 xl:grid-cols-[0.78fr_1.22fr]">
                        <div className="rounded-[2rem] bg-white p-6 shadow-sm sm:p-8">
                            <div className="inline-flex rounded-full bg-sky-50 px-4 py-2 text-xs font-black text-sky-700 ring-1 ring-sky-100">
                                本机读取 · 战绩查询 · 对局监听
                            </div>
                            <h2 className="mt-6 max-w-xl text-4xl font-black leading-[1.05] tracking-tight sm:text-6xl">
                                一个更干净的英雄联盟数据入口
                            </h2>
                            <p className="mt-5 max-w-xl text-sm leading-7 text-slate-600 sm:text-base">
                                Qiyana 是一个 LCU 客户端页面，连接已登录的英雄联盟客户端，把我的战绩面板、战绩查询、英雄选择监听和游戏中阵容信息整理成清晰的卡片视图。
                            </p>

                            <div className="mt-8 grid gap-3">
                                {features.map(([num, title, desc]) => (
                                    <div key={title} className="flex gap-4 rounded-3xl border border-slate-200 bg-slate-50 p-4">
                                        <div className="grid h-11 w-11 shrink-0 place-items-center rounded-2xl bg-white text-sm font-black text-sky-600 shadow-sm">{num}</div>
                                        <div>
                                            <h3 className="font-black">{title}</h3>
                                            <p className="mt-1 text-sm leading-6 text-slate-600">{desc}</p>
                                        </div>
                                    </div>
                                ))}
                            </div>

                            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                                <a
                                    href="/download/qiyana.exe"
                                    className="rounded-2xl bg-sky-600 px-6 py-3 text-center text-sm font-black text-white shadow-lg shadow-sky-200 transition hover:bg-sky-500"
                                >
                                    立即下载
                                </a>
                                <a
                                    href="#preview"
                                    className="rounded-2xl border border-slate-200 bg-white px-6 py-3 text-center text-sm font-black text-slate-900 transition hover:bg-slate-50"
                                >
                                    查看页面预览
                                </a>
                            </div>
                        </div>

                        <div className="grid gap-4 lg:grid-cols-[1fr_0.72fr]">
                            <div className="overflow-hidden rounded-[2rem] bg-[#111827] p-3 shadow-sm">
                                <div className="mb-3 flex items-center justify-between px-2 text-white">
                                    <div>
                                        <p className="text-xs font-bold text-sky-300">主面板</p>
                                        <h3 className="font-black">我的战绩面板</h3>
                                    </div>
                                    <span className="rounded-full bg-white/10 px-3 py-1 text-xs text-slate-200">LCU Connected</span>
                                </div>
                                <img
                                    src="/resource/1.png"
                                    alt="我的战绩面板截图"
                                    className="h-[420px] w-full rounded-[1.5rem] object-cover object-top"
                                />
                            </div>

                            <div className="grid gap-4">
                                <div className="rounded-[2rem] bg-white p-5 shadow-sm">
                                    <p className="text-xs font-black uppercase tracking-[0.2em] text-slate-400">No Password</p>
                                    <div className="mt-4 text-5xl font-black text-sky-600">0</div>
                                    <p className="mt-2 text-sm leading-6 text-slate-600">不需要输入英雄联盟账号密码，只读取本机已登录客户端数据。</p>
                                </div>
                                <div className="overflow-hidden rounded-[2rem] bg-white p-3 shadow-sm">
                                    <img src="/resource/2.png" alt="战绩查询截图" className="h-[205px] w-full rounded-[1.5rem] object-cover object-top" />
                                </div>
                            </div>
                        </div>
                    </section>

                    <section id="preview" className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
                        {screenshots.map((item) => (
                            <article key={item.src} className="overflow-hidden rounded-[2rem] bg-white p-3 shadow-sm">
                                <img src={item.src} alt={`${item.title}截图`} className="h-44 w-full rounded-[1.5rem] object-cover object-top" />
                                <div className="p-3">
                                    <div className="mb-2 inline-flex rounded-full bg-slate-100 px-3 py-1 text-xs font-black text-slate-600">{item.tag}</div>
                                    <h3 className="text-lg font-black">{item.title}</h3>
                                    <p className="mt-2 text-sm leading-6 text-slate-600">{item.desc}</p>
                                </div>
                            </article>
                        ))}
                    </section>

                    <section className="mt-4 rounded-[2rem] bg-[#111827] p-5 text-white shadow-sm sm:flex sm:items-center sm:justify-between sm:gap-6">
                        <div>
                            <h2 className="text-2xl font-black">启动英雄联盟客户端，然后打开 Qiyana</h2>
                            <p className="mt-2 text-sm leading-6 text-slate-300">用于日常复盘、战绩查询、赛前查看队友与对局监听。</p>
                        </div>
                        <a
                            href="/download/qiyana.exe"
                            className="mt-4 inline-flex shrink-0 rounded-2xl bg-white px-6 py-3 text-sm font-black text-[#111827] transition hover:bg-sky-100 sm:mt-0"
                        >
                            下载 Windows 版
                        </a>
                    </section>
                </div>
            </div>
        </main>
    )
}
