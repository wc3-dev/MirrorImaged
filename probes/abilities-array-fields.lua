-- The level array fields: every ability field id common.j names (each of its 860, and the ids the object data keeps
-- lists in) read as each of the four array kinds on four abilities, at levels 0 and 1 and indexes -1 to 3, and
-- then set, added to and removed from; a field that answered anything but the empty default is listed. The first
-- probe of abilities-fields tried five fields; this tries them all.
local ids = {}
for id in ([[achd aher aite Rtn1 Rtn2 Ntm4 sla2 Fae2 Slo3 ipv3 Gho1 Owk4 cyc1 Hds1 inv5 inv4 inv3 mfl6 Ans6 Bli2 Eah2 Ncl5 hsf6 irl5 Eth2 Gho3 inv2 hsf5 Ocr6 Sds6 Ear4 Ihid dvm6 Eth1 Gho2 Ssk5 Ssk4 Uan3 Ucb6 Udp5 Udp4 hgh5 Udp3 Ear2 coa2 Hbh5 Ocr5 irl4 Rej4 Arm2 Oar2 Hab2 Had2 Uau3 Roa6 Roa5 Hre2 Ear3 hhr7 hsl3 Mbt5 hic3 chr6 liq4 Roo3 spb2 Neu3 Neu4 Ens7 sla1 Btl2 Pos3 Pos4 Hmt3 Itp2 aubx auby abpx abpy arpx arpy acac alev alsk amsp apri arlv atac Sal2 Iagi Btl1 Loa1 irl3 Rej3 sol6 Emeu Iarp Nab3 Nsi1 Iatt Iaa1 sol7 swp9 Prg2 Nst1 uvg4 uvg3 uvg5 uvg2 Npr1 Nsa1 Car1 Ndc2 Gydu Idic Icfd swp5 Idid Har1 Idef Nrg6 Inf2 Roa2 Fae1 Ircd Nvcu Idet Det1 Fla1 Ifa1 Ndt3 Nsp3 Ofs1 gra3 uvg6 Iobu gra4 Iob5 Ixpg Icfx Nsyu Fla3 Nlm6 Har3 Ndt1 Nsp1 Igol Bgm1 Egm1 uvg1 Ihp2 Ihpg Ihpr Ihps Iint Neu2 Wha2 inv1 Ilev Har2 Ndt2 Nsp2 Ilum imou amcs Ams4 Idim Prg6 Imp2 Impg Imps Icfm gra5 Dev3 Icre Nch1 Ply1 Pos1 idc3 exh1 Gyd1 Efk3 spl2 Irec Itpm Ndp3 spb4 Uds1 Rpb6 Rpb5 udb5 ubr3 uwf1 Dev1 Ntm3 Gld1 Ilif Iman Bgm3 Uls3 swp7 Roa7 Ucb5 Tau3 hmc3 Ndp2 spb3 Gld3 Ncs3 Eme2 Imvb sol8 Prg1 ict1 ict2 Cha1 Cad1 Hre1 Uan1 Omi1 imo1 Esn4 Tau4 Hbz3 Efn1 Esv1 Nba2 Ndo2 Osf2 Uls1 Ocl2 Sod1 mec1 Nrc2 Hmt1 Hbz1 Ncl3 Aplu Tau2 Tau1 irc2 irc3 coau ent1 Nvc1 Roo1 Ams3 Isib Nfyu Spa1 Nlm3 Poa5 Poi4 Spo4 udb3 Hca4 Istr Nrg5 Hwe2 Npa5 Osf1 Nef1 Efnu Esvu Hwe1 Nbau Ndou Ntou Uin4 Isn1 Isn2 Ulsu uwf4 Ncl2 Wrs3 Rai1 Rai2 Nsl1 Chl1 exhu Raiu ipmu Igl1 Iglu Roo4 Roo2 hwdu Stau Nhs6 Nvc2 amac bld8 Shm3 imo2 Min1 Sta1 Neu1 Tsp1 Uim4 Ens2 Ens1 hcl3 hcl4 hcl5 hsa7 hcl1 hcr1 ula2 sol2 sol1 ula1 sol3 ula6 sol4 sol5 hsa3 hsa2 hsa1 ula4 ula5 ula3 Eme5 Eme3 Hhb1 Udc1 Oar1 Arm1 Omi4 Htc1 aare Ufn1 Had1 Ufa2 Ufa1 swpd swpc swp8 Wha3 Ncl4 gra1 swp3 Akb1 Cac1 Uav1 swp2 Ncr6 Uhf1 Def4 Esh3 Hca3 Poa3 Poi2 Slo2 Spo3 bsk2 Isx1 bld3 Blo1 Oae2 Nsi4 Ctc4 swpf swpe liq3 Cri2 Ens5 Hbn2 Htc4 Nab2 Nso5 Chd2 Apl1 Inf3 Mbt3 Owk3 Nst2 Emb2 Emb3 hgr4 hgh1 udb4 hgh7 hgh4 hgh3 chr3 hhr5 hhr4 ubr2 Nic1 hsl2 Ens6 hsf3 ubr1 Ndr7 Ndr6 Ndr9 Ndr8 hgh2 Eim3 Ncs5 Nvc4 Sds1 Esf3 Etq3 Hbz4 Hfs5 Udd2 hsf1 hsf2 Hmt2 Uds2 acas aran Hbh1 Ocr1 Def6 Nde1 Eev1 Ocr4 Iob3 Iob4 Iob2 Crs1 Hbh4 Nsi2 Ssk1 War1 Cmg3 ubr4 uwf2 acdn uwf5 swpb Nms2 Ncs1 Nso1 Nst3 Pos2 fak1 Hav3 Hbh3 Hfa1 hhr1 Idam ipv1 isr1 Nba1 Neg2 Ocr3 Ear1 swph swpg mfl5 Ctb1 Ctc1 Esf1 Def2 Iild Omi2 Eah1 Uim3 War2 Nst5 swp4 Hbz2 hsl1 Htb1 Nbr1 Inf1 Roa1 Esf2 Nab6 Ncs2 Nde2 Hbh2 Nde4 Ocr2 Nde3 Nfd3 Osh1 Nso3 swpa Nso2 Eim1 Apl2 Cor1 Dev2 Eer1 Hbz5 Idps Lsh1 mls1 Nbf5 Ndo1 Oww1 Poa2 Poi1 pxf2 Spo1 Tdg1 Oeq2 Uhf2 Efk1 Ocl1 Nst4 fbk4 fbk2 Iilw Cri3 isr2 bld1 Ocl3 bld6 Uls4 Uls5 swp1 bsk3 Def1 Omi3 Dtn2 Ixs1 hcl7 Ucs1 Uin1 uwf3 Wrs1 chr2 chr1 Shm2 Nic6 Nic2 Nic3 Nic4 Nic5 hhr6 Esh1 Esh4 Hav1 Uts3 hgr2 hic1 hgr1 Def7 Def8 Idel Sta2 Sta3 Osh3 Ucs3 nca1 spl1 Ndr3 Uin2 ahdu adur Esn5 Apl3 Eat2 Fla2 Oeq1 Ncs6 hcr2 ula8 hcr3 ula7 hsa6 hsa5 hsa4 hcl6 Bli1 hmc2 mcrd sfrd Hca1 liq1 Poa1 Ctc2 Shm1 Oeq4 Osh4 Ucs4 bld7 udb1 bns6 Ncl1 Dda2 flk3 Nvc5 Hfs1 Hfs2 Dda1 fak4 War3 Ntm1 Lit1 Nfd1 Lit2 Nfd2 Hfs3 Nvc6 Hfs4 fak5 War4 hic4 hcl2 swp6 bns7 hsf4 Etq2 hsl4 chr5 mfl2 mfl4 Prg5 Nsa2 Ndr1 Eat3 Hea1 Mbt2 Rej1 Can1 Nsa5 Hav2 Esn3 Esn2 Ssk3 Uin3 Esh5 pxf1 Etq4 Tau5 Bgm2 Egm2 Min2 Esn1 Eme4 Nfy2 Nsy5 Udp2 Udp1 Nlm5 Etq1 dvm3 dvm1 irl1 Uau2 Roa3 ave5 Inf4 ast1 ipv2 Ivam Ndr4 Ntm2 Wha1 imo3 mim1 Ams2 Def5 Hav4 Ixs2 Nsa4 Oww2 hgr3 Eim2 Nmr1 Mbt1 Adm1 Dtn1 idc1 dvm4 Nms1 hgh6 Cmg2 dvm2 Ndr2 Rej2 Roa4 irl2 Imrp Hab1 ast2 Ndr5 Hfs6 Osh2 Hbz6 abs1 abs2 Ebl1 Efk4 Efk2 Ncs4 Uco5 Ucs2 Nlm4 Can2 Udd1 Emb1 fbk3 fbk1 flk4 fak2 Tdg3 flk1 Tdg2 Ens3 mcdr Ssk2 Tsp2 Rpb3 Rpb4 Ebl2 Gld2 Def3 Esh2 Hca2 Poa4 Poi3 Slo1 Spo2 bsk1 Ispi Blo2 Oae1 Owk2 Uau1 Nsi3 Ctc3 liq2 bld2 Cri1 Hbn1 Htc3 Nab1 Nso4 Chd1 Ncr5 Neg1 Uco6 Rep5 chr4 bld4 Dda4 Dda3 slrd udb2 Rep3 Rep4 Nab4 Gyd3 Gyd2 Bgm4 Uts2 Ore1 gra2 Rep1 Rep2 Uts1 Eat1 Sal1 Blo3 Nab5 Nse1 hhc3 hhc1 hhc2 flk5 fak3 Tdg5 flk2 Tdg4 Nfy1 Nsy1 Nsy3 Nsy4 Htc2 Ufn2 hic2 Nlm2 Ens4 hhr3 hhr2 Sta4 Usl1 fbk5 Adm2 Ams1 Chd3 dvm5 idc2 Prg3 Npa6 Nba3 Ndo3 bld5 chra hhc4 Wrp2 Wrp1 Wrs2 Owk1 Ivs1 Oeq3 mfl1 mfl3 Prg4 Nsa3 Uls2 Mbt4 Uim1 Nvc3 Uim2 aefs aefl auar arar anam Nrc1 Neg3 Neg4 Neg5 Neg6 Mil2 aaea Ans5 Ncl6 spb5 acat tpi2 aeat aart alig amat Ply3 Ply4 Ply2 Ply5 Eme1 Mil1 coa1 dcp1 dcp2 Ndc1 tpi1 Ndp1 Nsy2 asat spb1 Ist1 Ist2 atat aret arut atp1 aub1 aut1 auu1 Rai3 Sod2 Rai4 abuf aeff atar areq arqa ata0 aani]]):gmatch("%S+") do ids[#ids + 1] = FourCC(id) end
local names = { "AHbz", "AHhb", "Aslo", "Absk" }
local spot = 0
local function with(id)
  spot = spot + 1
  local u = CreateUnit(Player(0), FourCC("Hpal"), spot * 200 - 1000, 0, 0)
  UnitAddAbility(u, FourCC(id))
  return BlzGetUnitAbility(u, FourCC(id))
end
local abilities = {}
for _, id in ipairs(names) do abilities[#abilities + 1] = with(id) end

local get = {
  { BlzGetAbilityIntegerLevelArrayField, ConvertAbilityIntegerLevelArrayField, 0 },
  { BlzGetAbilityRealLevelArrayField, ConvertAbilityRealLevelArrayField, 0.0 },
  { BlzGetAbilityBooleanLevelArrayField, ConvertAbilityBooleanLevelArrayField, false },
  { BlzGetAbilityStringLevelArrayField, ConvertAbilityStringLevelArrayField, "" },
}
local set = {
  { BlzSetAbilityIntegerLevelArrayField, 1 },
  { BlzSetAbilityRealLevelArrayField, 1.5 },
  { BlzSetAbilityBooleanLevelArrayField, true },
  { BlzSetAbilityStringLevelArrayField, "Bslo" },
}
local add = {
  { BlzAddAbilityIntegerLevelArrayField, 1 },
  { BlzAddAbilityRealLevelArrayField, 1.5 },
  { BlzAddAbilityBooleanLevelArrayField, true },
  { BlzAddAbilityStringLevelArrayField, "Bslo" },
}
local remove = {
  { BlzRemoveAbilityIntegerLevelArrayField, 1 },
  { BlzRemoveAbilityRealLevelArrayField, 1.5 },
  { BlzRemoveAbilityBooleanLevelArrayField, true },
  { BlzRemoveAbilityStringLevelArrayField, "Bslo" },
}

-- Reads of every field of every kind at levels 0 and 1, indexes -1 to 3; the number read and the first few that
-- weren't the default.
local function sweep(label)
  local reads, odd = 0, {}
  for a, ability in ipairs(abilities) do
    for _, id in ipairs(ids) do
      for k, g in ipairs(get) do
        local field = g[2](id)
        for level = 0, 1 do
          for index = -1, 3 do
            reads = reads + 1
            local value = g[1](ability, field, level, index)
            if value ~= g[3] and #odd < 8 then odd[#odd + 1] = names[a] .. " " .. id .. " kind " .. k .. " level " .. level .. " index " .. index .. " " .. tostring(value) end
          end
        end
      end
    end
  end
  out(label .. " reads " .. reads .. " other than the default " .. #odd)
  for _, line in ipairs(odd) do out(label .. " " .. line) end
end

local function change(label, list, extra)
  local calls, trues = 0, 0
  for _, ability in ipairs(abilities) do
    for _, id in ipairs(ids) do
      for k, g in ipairs(list) do
        local field = get[k][2](id)
        for level = 0, 1 do
          calls = calls + 1
          if (extra and g[1](ability, field, level, 0, g[2]) or not extra and g[1](ability, field, level, g[2])) then trues = trues + 1 end
        end
      end
    end
  end
  out(label .. " calls " .. calls .. " true " .. trues)
end

sweep("read")
change("set", set, true)
change("add", add, false)
sweep("read after")
change("remove", remove, false)
sweep("read last")
done()
